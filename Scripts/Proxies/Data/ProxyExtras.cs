#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    [CreateAssetMenu(menuName = "EditorVR/ProxyExtras")]

    /// <summary>
    /// Spawn additional objects around a proxy node
    /// </summary>
    sealed class ProxyExtras : ScriptableObject
    {
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

        Dictionary<Node, List<GameObject>> m_Data;

        [SerializeField]
        ProxyExtraData[] m_Extras;
    }
}
#endif
