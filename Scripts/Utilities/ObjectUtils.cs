using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    /// <summary>
    /// Object related EditorVR utilities
    /// </summary>
    static class ObjectUtils
    {
        public static HideFlags hideFlags
        {
            get { return s_HideFlags; }
            set { s_HideFlags = value; }
        }

        static HideFlags s_HideFlags = HideFlags.DontSaveInEditor;

        // Local method use only -- created here to reduce garbage collection
        static readonly List<Renderer> k_Renderers = new List<Renderer>();
        static readonly List<Transform> k_Transforms = new List<Transform>();

        public static GameObject Instantiate(GameObject prefab, Transform parent = null, bool worldPositionStays = true,
            bool runInEditMode = true, bool active = true)
        {
            var go = UnityObject.Instantiate(prefab, parent, worldPositionStays);
            if (worldPositionStays)
            {
                var goTransform = go.transform;
                var prefabTransform = prefab.transform;
                goTransform.position = prefabTransform.position;
                goTransform.rotation = prefabTransform.rotation;
            }

            go.SetActive(active);
            if (!Application.isPlaying && runInEditMode)
            {
                SetRunInEditModeRecursively(go, runInEditMode);
                go.hideFlags = hideFlags;
            }

            return go;
        }

        /// <summary>
        /// Create an empty VR GameObject.
        /// </summary>
        /// <param name="name">Name of the new GameObject</param>
        /// <param name="parent">Transform to parent new object under</param>
        /// <returns>The newly created empty GameObject</returns>
        public static GameObject CreateEmptyGameObject(string name = null, Transform parent = null)
        {
            GameObject empty = null;
            if (string.IsNullOrEmpty(name))
                name = "New Game Object";

#if UNITY_EDITOR
            empty = EditorUtility.CreateGameObjectWithHideFlags(name, hideFlags);
#else
            empty = new GameObject(name);
            empty.hideFlags = hideFlags;
#endif
            empty.transform.parent = parent;
            empty.transform.localPosition = Vector3.zero;

            return empty;
        }

        public static T CreateGameObjectWithComponent<T>(Transform parent = null, bool worldPositionStays = true,
            bool runInEditMode = true)
            where T : Component
        {
            return (T)CreateGameObjectWithComponent(typeof(T), parent, worldPositionStays, runInEditMode);
        }

        public static Component CreateGameObjectWithComponent(Type type, Transform parent = null,
            bool worldPositionStays = true, bool runInEditMode = true)
        {
            Component component = null;
            if (Application.isPlaying)
            {
                var go = new GameObject(type.Name);
                go.transform.parent = parent;
                component = AddComponent(type, go);
            }
#if UNITY_EDITOR
            else
            {
                var go = EditorUtility.CreateGameObjectWithHideFlags(type.Name, hideFlags, type);
                component = go.GetComponent(type);
                if (component)
                {
                    SetRunInEditModeRecursively(component.gameObject, runInEditMode);
                    component.transform.SetParent(parent, worldPositionStays);
                }
                else
                {
                    UnityObject.DestroyImmediate(go);
                }
            }
#endif

            return component;
        }

        public static Bounds GetBounds(List<GameObject> gameObjects)
        {
            Bounds? bounds = null;
            foreach (var gameObject in gameObjects)
            {
                var goBounds = GetBounds(gameObject.transform);
                if (!bounds.HasValue)
                {
                    bounds = goBounds;
                }
                else
                {
                    goBounds.Encapsulate(bounds.Value);
                    bounds = goBounds;
                }
            }

            return bounds ?? new Bounds();
        }

        public static Bounds GetBounds(Transform[] transforms)
        {
            Bounds? bounds = null;
            foreach (var t in transforms)
            {
                var goBounds = GetBounds(t);
                if (!bounds.HasValue)
                {
                    bounds = goBounds;
                }
                else
                {
                    goBounds.Encapsulate(bounds.Value);
                    bounds = goBounds;
                }
            }
            return bounds ?? new Bounds();
        }

        public static Bounds GetBounds(Transform transform)
        {
            k_Renderers.Clear();
            transform.GetComponentsInChildren(k_Renderers);
            var b = GetBounds(k_Renderers);

            // As a fallback when there are no bounds, collect all transform positions
            if (b.size == Vector3.zero)
            {
                k_Transforms.Clear();
                transform.GetComponentsInChildren(k_Transforms);

                if (k_Transforms.Count > 0)
                    b.center = k_Transforms[0].position;

                foreach (var t in k_Transforms)
                {
                    b.Encapsulate(t.position);
                }
            }

            return b;
        }

        public static Bounds GetBounds(List<Renderer> renderers)
        {
            if (renderers.Count > 0)
            {
                var first = renderers[0];
                var b = new Bounds(first.transform.position, Vector3.zero);
                foreach (var r in renderers)
                {
                    if (r.bounds.size != Vector3.zero)
                        b.Encapsulate(r.bounds);
                }

                return b;
            }

            return default(Bounds);
        }

        public static void SetRunInEditModeRecursively(GameObject go, bool enabled)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            var monoBehaviours = go.GetComponents<MonoBehaviour>();
            foreach (var mb in monoBehaviours)
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
            Component component = null;
            if (Application.isPlaying)
            {
                var mb = DefaultScriptReferences.Create(type);
                if (mb)
                {
                    mb.transform.parent = go.transform;
                    mb.enabled = true;
                    component = mb;
                }
            }

            if (!component)
                component = go.AddComponent(type);

            SetRunInEditModeRecursively(go, true);
            return component;
        }

        public static T CopyComponent<T>(T sourceComponent, GameObject targetGameObject) where T : Component
        {
            var sourceType = sourceComponent.GetType();
            var clonedTargetComponent = AddComponent(sourceType, targetGameObject);
            if (Application.isPlaying)
            {
                Type type = sourceComponent.GetType();
                var fields = type.GetFields();
                foreach (var field in fields)
                {
                    if (field.IsStatic)
                        continue;

                    field.SetValue(clonedTargetComponent, field.GetValue(sourceComponent));
                }

                var props = type.GetProperties();
                foreach (var prop in props)
                {
                    if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name")
                        continue;

                    prop.SetValue(clonedTargetComponent, prop.GetValue(sourceComponent, null), null);
                }
            }
#if UNITY_EDITOR
            else
            {
                EditorUtility.CopySerialized(sourceComponent, clonedTargetComponent);
            }
#endif
            return (T)clonedTargetComponent;
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
                    // Skip any assemblies that don't load properly -- suppress errors
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

            return Enumerable.Empty<Type>();
        }

        public static IEnumerable<Type> GetExtensionsOfClass(Type type)
        {
            if (type.IsClass)
                return GetAssignableTypes(type);

            return Enumerable.Empty<Type>();
        }

        public static void Destroy(UnityObject o, float t = 0f, bool withUndo = false)
        {
            if (Application.isPlaying)
            {
                UnityObject.Destroy(o, t);
            }
#if UNITY_EDITOR
            else
            {
                if (Mathf.Approximately(t, 0f))
                {
                    if (withUndo)
                        Undo.DestroyObjectImmediate(o);
                    else
                        UnityObject.DestroyImmediate(o);
                }
                else
                {
                    VRView.StartCoroutine(DestroyInSeconds(o, t));
                }
            }
#endif
        }

        static IEnumerator DestroyInSeconds(UnityObject o, float t, bool withUndo = false)
        {
            var startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup <= startTime + t)
                yield return null;

#if UNITY_EDITOR
            if (withUndo)
                Undo.DestroyObjectImmediate(o);
            else
#endif
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
        /// TODO: expose internal SerialzedProperty.ValidateObjectReferenceValue to remove his hack
        /// </summary>
        /// <param name="name">Weak type name</param>
        /// <returns>Best guess System.Type</returns>
        public static Type TypeNameToType(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Equals(name) && typeof(UnityObject).IsAssignableFrom(type))
                            return type;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly
                }
            }

            return typeof(UnityObject);
        }

#if UNITY_EDITOR
        public static IEnumerator GetAssetPreview(UnityObject obj, Action<Texture> callback)
        {
            var texture = AssetPreview.GetAssetPreview(obj);

            while (AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID()))
            {
                texture = AssetPreview.GetAssetPreview(obj);
                yield return null;
            }

            if (!texture)
                texture = AssetPreview.GetMiniThumbnail(obj);

            callback(texture);
        }
#endif
    }
}
