using System;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.EditorXR.Utilities
{
    static class EditorXRUtils
    {
        static HideFlags s_HideFlags = HideFlags.DontSaveInEditor;

        public static HideFlags hideFlags
        {
            get { return s_HideFlags; }
            set { s_HideFlags = value; }
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
                go.SetRunInEditModeRecursively(true);

            go.SetHideFlagsRecursively(hideFlags);

            return go;
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
#if UNITY_EDITOR
            if (Application.isPlaying)
                return RuntimeCreateGameObjectWithComponent(type, parent, worldPositionStays);

            var go = EditorUtility.CreateGameObjectWithHideFlags(type.Name, hideFlags, type);
            var component = go.GetComponent(type);
            if (component)
            {
                component.gameObject.SetRunInEditModeRecursively(runInEditMode);
                component.transform.SetParent(parent, worldPositionStays);
            }
            else
            {
                UnityObject.DestroyImmediate(go);
            }

            return component;
#else
            return RuntimeCreateGameObjectWithComponent(type, parent, worldPositionStays);
#endif
        }

        static Component RuntimeCreateGameObjectWithComponent(Type type, Transform parent = null,
            bool worldPositionStays = true)
        {
            var go = new GameObject(type.Name);
            go.hideFlags = hideFlags;
            go.transform.SetParent(parent, worldPositionStays);
            return AddComponent(type, go);
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

            go.SetRunInEditModeRecursively(true);
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
    }
}
