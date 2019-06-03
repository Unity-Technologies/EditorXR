using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    public class DefaultScriptReferences : ScriptableObject
    {
        const string k_ParentFolder = "Assets";
        const string k_ResourcesFolder = "Resources";
        const string k_Path = k_ParentFolder + "/" + k_ResourcesFolder + "/DefaultScriptReferences.asset";

#pragma warning disable 649
        [SerializeField]
        GameObject m_ScriptPrefab;

        [SerializeField]
        List<ScriptableObject> m_EditingContexts;
#pragma warning restore 649

        Dictionary<Type, GameObject> m_TypePrefabs = new Dictionary<Type, GameObject>();

        internal static MonoBehaviour Create(Type type)
        {
            var defaultScriptReferences = Resources.Load<DefaultScriptReferences>(Path.GetFileNameWithoutExtension(k_Path));
            if (defaultScriptReferences)
            {
                GameObject prefab;
                if (defaultScriptReferences.m_TypePrefabs.TryGetValue(type, out prefab))
                {
                    var go = Instantiate(prefab);
                    go.SetHideFlagsRecursively(EditorXRUtils.hideFlags);
                    return (MonoBehaviour)go.GetComponent(type);
                }
            }

            return null;
        }

        internal static List<IEditingContext> GetEditingContexts()
        {
            var defaultScriptReferences = Resources.Load<DefaultScriptReferences>(Path.GetFileNameWithoutExtension(k_Path));
            return defaultScriptReferences ? defaultScriptReferences.m_EditingContexts.ConvertAll(ec => (IEditingContext)ec) : null;
        }

        void Awake()
        {
            if (m_ScriptPrefab)
            {
                foreach (Transform typePrefab in m_ScriptPrefab.transform)
                {
                    var components = typePrefab.GetComponents<MonoBehaviour>();
                    foreach (var c in components)
                    {
                        if (c)
                            m_TypePrefabs[c.GetType()] = typePrefab.gameObject;
                    }
                }
            }
        }

#if UNITY_2018_3_OR_NEWER
#if UNITY_EDITOR
        [MenuItem("Assets/Create/EditorXR/Default Script References")]
        static void UpdateScriptReferences()
        {
            var defaultScriptReferences = CreateInstance<DefaultScriptReferences>();

            var prefabsRoot = new GameObject(Path.GetFileNameWithoutExtension(k_Path));
            prefabsRoot.SetActive(false);
            Action<ICollection> create = types =>
            {
                foreach (Type t in types)
                {
                    if (t.IsNested || !typeof(MonoBehaviour).IsAssignableFrom(t))
                        continue;

                    if (t.GetCustomAttributes(true).OfType<EditorOnlyWorkspaceAttribute>().Any())
                        continue;

                    var mb = (MonoBehaviour)EditorXRUtils.CreateGameObjectWithComponent(t, prefabsRoot.transform, runInEditMode: false);
                    if (mb)
                    {
                        mb.gameObject.hideFlags = HideFlags.None;
                        mb.enabled = false;
                        mb.transform.parent = prefabsRoot.transform;
                    }
                }
            };

            var defaultReferenceTypes = new List<Type>();
            typeof(IEditor).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IProxy).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(ITool).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IModule).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IMainMenu).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IToolsMenu).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IAlternateMenu).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IAction).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IWorkspace).GetImplementationsOfInterface(defaultReferenceTypes);
            typeof(IScriptReference).GetImplementationsOfInterface(defaultReferenceTypes);
            create(defaultReferenceTypes);

            var directory = Path.GetDirectoryName(k_Path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            defaultScriptReferences.m_ScriptPrefab = PrefabUtility.SaveAsPrefabAsset(prefabsRoot, Path.ChangeExtension(k_Path, "prefab"));
            defaultScriptReferences.m_EditingContexts = EditingContextManager.GetEditingContextAssets().ConvertAll(ec => (ScriptableObject)ec);

            AssetDatabase.CreateAsset(defaultScriptReferences, k_Path);

            DestroyImmediate(prefabsRoot);

            AssetDatabase.SaveAssets();
        }
#endif
#endif
    }
}
