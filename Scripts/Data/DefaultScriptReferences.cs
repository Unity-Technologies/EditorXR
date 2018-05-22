using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    public class DefaultScriptReferences : ScriptableObject
    {
        const string k_Path = "Assets/Resources/DefaultScriptReferences.asset";

        [SerializeField]
        GameObject m_ScriptPrefab;

        [SerializeField]
        List<ScriptableObject> m_EditingContexts;

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
                            m_TypePrefabs.Add(c.GetType(), typePrefab.gameObject);
                    }
                }
            }
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/EditorXR/Default Script References")]
        static void UpdateScriptReferences()
        {
            var defaultScriptReferences = CreateInstance<DefaultScriptReferences>();

            var prefabsRoot = new GameObject(System.IO.Path.GetFileNameWithoutExtension(k_Path));
            Action<ICollection> create = (types) =>
            {
                foreach (Type t in types)
                {
                    if (t.IsNested || !typeof(MonoBehaviour).IsAssignableFrom(t))
                        continue;

                    var mb = (MonoBehaviour)ObjectUtils.CreateGameObjectWithComponent(t, runInEditMode: false);
                    mb.gameObject.hideFlags = HideFlags.None;
                    mb.enabled = false;
                    mb.transform.parent = prefabsRoot.transform;
                }
            };

            create(ObjectUtils.GetImplementationsOfInterface(typeof(IEditor)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(IProxy)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(ITool)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(ISystemModule)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(IMainMenu)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(IToolsMenu)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(IAlternateMenu)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(IAction)).ToList());
            create(ObjectUtils.GetImplementationsOfInterface(typeof(IWorkspace)).ToList());

            defaultScriptReferences.m_ScriptPrefab = PrefabUtility.CreatePrefab(Path.ChangeExtension(k_Path, "prefab"), prefabsRoot);
            defaultScriptReferences.m_EditingContexts = EditingContextManager.GetEditingContextAssets().ConvertAll(ec => (ScriptableObject)ec);

            if (!Directory.Exists(k_Path))
                Directory.CreateDirectory(Path.GetDirectoryName(k_Path));
            AssetDatabase.CreateAsset(defaultScriptReferences, k_Path);

            DestroyImmediate(prefabsRoot);

            AssetDatabase.SaveAssets();
        }
#endif
    }
}
