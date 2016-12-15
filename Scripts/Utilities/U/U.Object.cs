using System.Reflection;
using UnityEngine.Networking;

namespace UnityEngine.Experimental.EditorVR.Utilities
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
	using UnityEditor.Experimental.EditorVR;
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

			public static T CreateGameObjectWithComponent<T>(Transform parent = null) where T : Component
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

			public static Bounds GetBounds(GameObject[] gameObjects)
			{
				Bounds? bounds = null;
				foreach (var go in gameObjects)
				{
					var goBounds = GetBounds(go);
					if (!bounds.HasValue)
					{
						bounds = goBounds;
					} else
					{
						goBounds.Encapsulate(bounds.Value);
						bounds = goBounds;
					}
				}
				return bounds ?? new Bounds();
			}

			public static Bounds GetBounds(GameObject obj)
			{
				Bounds b = new Bounds(obj.transform.position, Vector3.zero);
				Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
				foreach (Renderer r in renderers)
				{
					if (r.bounds.size != Vector3.zero)
						b.Encapsulate(r.bounds);
				}

				// As a fallback when there are no bounds, collect all transform positions
				if (b.size == Vector3.zero)
				{
					var transforms = obj.GetComponentsInChildren<Transform>();
					foreach (var t in transforms)
						b.Encapsulate(t.position);
				}

				return b;
			}

			public static void SetRunInEditModeRecursively(GameObject go, bool enabled)
			{
#if UNITY_EDITOR && UNITY_EDITORVR
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
#endif
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

			static IEnumerable<Type> GetAssignableTypes(Type type, Func<Type, bool> predicate = null)
			{
				var list = new List<Type>();
				ForEachType(t =>
				{
					if (type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && (predicate == null || predicate(t)))
						list.Add(t);
				});

				return list;
			}

			public static void ForEachAssembly(Action<Assembly> callback)
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var assembly in assemblies)
				{
					try
					{
						callback(assembly);
					}
					catch (ReflectionTypeLoadException)
					{
						// Skip any assemblies that don't load properly
						continue;
					}
				}
			}

			public static void ForEachType(Action<Type> callback)
			{
				ForEachAssembly(assembly =>
				{
					var types = assembly.GetTypes();
					foreach (var t in types)
						callback(t);
				});
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
#if UNITY_EDITOR && UNITY_EDITORVR
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

			/// <summary>
			/// Strip "PPtr<> and $ from a string for getting a System.Type from SerializedProperty.type
			/// TODO: expose internal SerializedProperty.objectReferenceTypeString to remove this hack
			/// </summary>
			/// <param name="type">Type string</param>
			/// <returns>Nicified type string</returns>
			public static string NicifySerializedPropertyType(string type)
			{
				return type.Replace("PPtr<", "").Replace(">", "").Replace("$", "");
			}

			/// <summary>
			/// Search through all assemblies in the current AppDomain for a class that is assignable to UnityObject and matches the given weak name
			/// TODO: expoose internal SerialzedProperty.ValidateObjectReferenceValue to remove his hack
			/// </summary>
			/// <param name="name">Weak type name</param>
			/// <returns>Best guess System.Type</returns>
			public static Type TypeNameToType(string name)
			{
				return AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(x => x.GetTypes())
					.FirstOrDefault(x => x.Name.Equals(name) && typeof(UnityObject).IsAssignableFrom(x));
			}

			public static IEnumerator GetAssetPreview(UnityObject obj, Action<Texture> callback)
			{
				Texture texture = null;

#if UNITY_EDITOR
				texture = AssetPreview.GetAssetPreview(obj);

				while (AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID()))
				{
					texture = AssetPreview.GetAssetPreview(obj);
					yield return null;
				}

				if (!texture)
					texture = AssetPreview.GetMiniThumbnail(obj);
#else
				yield return null;
#endif

				callback(texture);
			}
		}
	}
}