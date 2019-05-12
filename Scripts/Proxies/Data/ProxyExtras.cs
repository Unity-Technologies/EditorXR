using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    /// <summary>
    /// Spawn additional objects around a proxy node
    /// </summary>
    [CreateAssetMenu(menuName = "EditorXR/Proxy Extras")]
    sealed class ProxyExtras : ScriptableObject
    {
#pragma warning disable 649
        [Serializable]
        struct ProxyExtraData
        {
            /// <summary>
            /// The proxy node to spawn this extra on
            /// </summary>
            public Node node;

            /// <summary>
            /// Prefab to spawn
            /// </summary>
            public GameObject prefab;
        }

        [SerializeField]
        ProxyExtraData[] m_Extras;
#pragma warning restore 649

        Dictionary<Node, List<GameObject>> m_Data;

        public Dictionary<Node, List<GameObject>> data
        {
            get
            {
                if (m_Data == null)
                {
                    m_Data = new Dictionary<Node, List<GameObject>>();
                    foreach (var extra in m_Extras)
                    {
                        var node = extra.node;
                        List<GameObject> prefabs;
                        if (!m_Data.TryGetValue(node, out prefabs))
                        {
                            prefabs = new List<GameObject>();
                            m_Data[node] = prefabs;
                        }

                        prefabs.Add(extra.prefab);
                    }
                }

                return m_Data;
            }
        }
    }
}
