namespace UnityEngine.VR.Utilities
{
	using System;
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UMaterial = UnityEngine.Material;
	using UObject = UnityEngine.Object;
#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.VR;
#endif

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public partial class U
	{
		/// <summary>
		/// Object related EditorVR utilities
		/// </summary>
		public class Object
		{
			public static GameObject ClonePrefab(GameObject prefab, GameObject parent = null)
			{
				GameObject obj = InstantiateAndSetActive(prefab);
				if (parent != null) SetParent(obj, parent);
				obj.transform.localPosition = new Vector3();
				return obj;
			}

			public static GameObject ClonePrefabByName(string resource, GameObject parent = null)
			{
				return ClonePrefab(Resources.Load<GameObject>(resource), parent);
			}

			public static GameObject InstantiateAndSetActive(GameObject prefab, Transform parent = null, bool worldPositionStays = true, bool runInEditMode = true)
			{
				GameObject go = UObject.Instantiate(prefab);
				go.transform.SetParent(parent, worldPositionStays);
				go.SetActive(true);
#if UNITY_EDITOR
				if (!Application.isPlaying && runInEditMode)
				{
					SetRunInEditModeRecursively(go, runInEditMode);
					go.hideFlags = EditorVR.kDefaultHideFlags;
				}
#endif
				return go;
			}

			public static void SetParent(GameObject obj, GameObject parent)
			{
				obj.transform.parent = parent.transform;
			}

			public static void Show(GameObject obj)
			{
				obj.SetActive(true);
			}

			public static void Hide(GameObject obj)
			{
				obj.SetActive(false);
			}

			public static void RemoveAllChildren(GameObject obj)
			{
				var children = new List<GameObject>();
				foreach (Transform child in obj.transform) children.Add(child.gameObject);
				foreach (GameObject child in children) UObject.Destroy(child);
			}

			public static bool IsInLayer(GameObject o, string s)
			{
				return o.layer == LayerMask.NameToLayer(s);
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

			public static IEnumerable<Type> GetImplementationsOfInterface(Type type)
			{
				if (type.IsInterface)
				{
					return AppDomain.CurrentDomain.GetAssemblies()
						.SelectMany(s => s.GetTypes())
						.Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
				}
				return new List<Type>();
			}
			
			public static void Destroy(UObject o, float t = 0f)
			{
				if (Application.isPlaying)
				{
					Object.Destroy(o, t);
				}
#if UNITY_EDITOR
				else
				{
					if (Mathf.Approximately(t, 0f))
						UObject.DestroyImmediate(o);
					else
					{
						EditorVRView.StartCoroutine(DestroyInSeconds(o, t));
					}
				}
#endif
			}

			private static IEnumerator DestroyInSeconds(UObject o, float t)
			{
				float startTime = Time.realtimeSinceStartup;
				while (Time.realtimeSinceStartup <= startTime + t)
					yield return null;

				UObject.DestroyImmediate(o);
			}
			
			public static GameObject SpawnGhostWireframe(GameObject obj, UMaterial ghostMaterial, bool enableRenderers = true)
			{
				// spawn ghost
				GameObject ghostObj = InstantiateAndSetActive(obj, obj.transform.parent);
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
			private static void GenerateWireframe(Renderer r, UMaterial ghostMaterial)
			{
				if (r)
				{
					UMaterial[] materials = r.sharedMaterials;
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
		}
	}
}