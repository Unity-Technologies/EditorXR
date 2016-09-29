using System.Reflection;

namespace UnityEngine.VR.Utilities
{
	using System;
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityMaterial = UnityEngine.Material;
	using UnityObject = UnityEngine.Object;
#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.VR;
#endif

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public static partial class U
	{
		/// <summary>
		/// Object related EditorVR utilities
		/// </summary>
		public static class Object
		{
			public static GameObject Instantiate(GameObject prefab, Transform parent = null, bool worldPositionStays = true, bool runInEditMode = true, bool active = true)
			{
				GameObject go = UnityObject.Instantiate(prefab);
				go.transform.SetParent(parent, worldPositionStays);
				go.SetActive(active);
#if UNITY_EDITOR
				if (!Application.isPlaying && runInEditMode)
				{
					SetRunInEditModeRecursively(go, runInEditMode);
					go.hideFlags = EditorVR.kDefaultHideFlags;
				}
#endif
				return go;
			}

			public static void RemoveAllChildren(GameObject obj)
			{
				var children = new List<GameObject>();
				foreach (Transform child in obj.transform) children.Add(child.gameObject);
				foreach (GameObject child in children) UnityObject.Destroy(child);
			}

			public static bool IsInLayer(GameObject o, string s)
			{
				return o.layer == LayerMask.NameToLayer(s);
			}

			/// <summary>
			/// Create an empty VR GameObject.
			/// </summary>
			/// <param name="name">Name of the new GameObject</param>
			/// <param name="parent">Transform to parent new object under</param>
			/// <returns>The newly created empty GameObject</returns>
			public static GameObject CreateEmptyGameObject(String name = null, Transform parent = null)
			{
				GameObject empty = null;
				if (String.IsNullOrEmpty(name))
					name = "Empty";
#if UNITY_EDITOR
				empty = EditorUtility.CreateGameObjectWithHideFlags(name, EditorVR.kDefaultHideFlags);
#else
				empty = new GameObject(name);
#endif
				empty.transform.parent = parent;
				empty.transform.localPosition = Vector3.zero;

				return empty;
			}

			public static T CreateGameObjectWithComponent<T>(Transform parent = null) where T : MonoBehaviour
			{
				return (T)CreateGameObjectWithComponent(typeof(T), parent);
			}

			public static Component CreateGameObjectWithComponent(Type type, Transform parent = null)
			{
#if UNITY_EDITOR
				Component component = EditorUtility.CreateGameObjectWithHideFlags(type.Name, EditorVR.kDefaultHideFlags, type).GetComponent(type);
				if (!Application.isPlaying)
					SetRunInEditModeRecursively(component.gameObject, true);
#else
				Component component = new GameObject(type.Name).AddComponent(type);
#endif
				component.transform.parent = parent;

				return component;
			}

			public static void SetLayerRecursively(GameObject root, int layer)
			{
				Transform[] transforms = root.GetComponentsInChildren<Transform>();
				for (int i = 0; i < transforms.Length; i++)
					transforms[i].gameObject.layer = layer;
			}

			public static Bounds GetBounds(GameObject obj)
			{
				Bounds b = new Bounds(obj.transform.position, Vector3.zero);
				Renderer[] childrenR = obj.GetComponentsInChildren<Renderer>();
				foreach (Renderer childR in childrenR)
				{
					b.Encapsulate(childR.bounds);
				}
				return b;
			}

			public static void SetRunInEditModeRecursively(GameObject go, bool enabled)
			{
				MonoBehaviour[] monoBehaviours = go.GetComponents<MonoBehaviour>();
				foreach (MonoBehaviour mb in monoBehaviours)
				{
					if (mb)
						mb.runInEditMode = enabled;
				}

				foreach (Transform child in go.transform)
				{
					SetRunInEditModeRecursively(child.gameObject, enabled);
				}
			}

			public static bool IsEditModeActive(MonoBehaviour mb)
			{
				return !Application.isPlaying && mb.runInEditMode;
			}

			public static T AddComponent<T>(GameObject go) where T : Component
			{
				return (T)AddComponent(typeof(T), go);
			}

			public static Component AddComponent(Type type, GameObject go)
			{
				Component component = go.AddComponent(type);
				SetRunInEditModeRecursively(go, true);
				return component;
			}

			private static IEnumerable<Type> GetAssignableTypes(Type type)
			{
				var list = new List<Type>();
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var assembly in assemblies)
				{
					Type[] types;
					try
					{
						types = assembly.GetTypes();
					}
					catch (ReflectionTypeLoadException)
					{
						// Skip any assemblies that don't load properly
						continue;
					}

					foreach (var t in types)
					{
						if (type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
							list.Add(t);
					}
				}

				return list;

			}

			public static IEnumerable<Type> GetImplementationsOfInterface(Type type)
			{
				
				if (type.IsInterface)
					return GetAssignableTypes(type);
				else
					return Enumerable.Empty<Type>();
			}

			public static IEnumerable<Type> GetExtensionsOfClass(Type type)
			{
				if (type.IsClass)
					return GetAssignableTypes(type);
				else
					return Enumerable.Empty<Type>();
			}

			public static void Destroy(UnityObject o, float t = 0f)
			{
				if (Application.isPlaying)
				{
					UnityObject.Destroy(o, t);
				}
#if UNITY_EDITOR
				else
				{
					if (Mathf.Approximately(t, 0f))
						UnityObject.DestroyImmediate(o);
					else
					{
						VRView.StartCoroutine(DestroyInSeconds(o, t));
					}
				}
#endif
			}

			private static IEnumerator DestroyInSeconds(UnityObject o, float t)
			{
				float startTime = Time.realtimeSinceStartup;
				while (Time.realtimeSinceStartup <= startTime + t)
					yield return null;

				UnityObject.DestroyImmediate(o);
			}

			public static GameObject SpawnGhostWireframe(GameObject obj, UnityMaterial ghostMaterial, bool enableRenderers = true)
			{
				// spawn ghost
				GameObject ghostObj = Instantiate(obj, obj.transform.parent);
				// generate wireframe for objects in tree containing renderers
				Renderer[] children = ghostObj.GetComponentsInChildren<Renderer>();
				foreach (Renderer r in children)
				{
					GenerateWireframe(r, ghostMaterial);
					r.enabled = enableRenderers;
				}
				ghostObj.transform.position = obj.transform.position;
				ghostObj.transform.rotation = obj.transform.rotation;
				ghostObj.transform.localScale = obj.transform.localScale;

				// remove colliders if there are any
				Collider[] colliders = ghostObj.GetComponents<Collider>();
				foreach (Collider c in colliders)
					Destroy(c);

				return ghostObj;
			}

			// generates wireframe if contains a renderer 
			private static void GenerateWireframe(Renderer r, UnityMaterial ghostMaterial)
			{
				if (r)
				{
					UnityMaterial[] materials = r.sharedMaterials;
					for (int i = 0; i < materials.Length; i++)
						materials[i] = ghostMaterial;
					r.sharedMaterials = materials;

					// generate wireframe
					MeshFilter mf = r.GetComponent<MeshFilter>();
					if (mf)
					{
						// TODO: Replace with new wireframe generator
						//Mesh mesh = mf.sharedMesh;
						// mf.mesh = WireframeGenerator.Generate(ref mesh, WIRE_INSIDE.Color);
					}
				}
			}

			public static Bounds? GetTotalBounds(Transform t)
			{
				Bounds? bounds = null;
				var renderers = t.GetComponentsInChildren<Renderer>(true);
				foreach (var renderer in renderers)
				{
					if (bounds == null)
						bounds = renderer.bounds;
					else
					{
						Bounds b = bounds.Value;
						b.Encapsulate(renderer.bounds);
						bounds = b;
					}
				}
				return bounds;
			}

			public static string NiceSerializedPropertyType(string type)
			{
				return type.Replace("PPtr<", "").Replace(">", "");
			}
		}
	}
}