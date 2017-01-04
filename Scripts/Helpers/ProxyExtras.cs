using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Tools;

/// <summary>
/// Spawn additional objects around a proxy node
/// </summary>
public class ProxyExtras : ScriptableObject
{
#if UNITY_EDITOR
	[MenuItem("Assets/Create/ScriptableObjects/ProxyExtras")]
	public static void Create()
	{
		var path = AssetDatabase.GetAssetPath(Selection.activeObject);

		if (string.IsNullOrEmpty(path))
			path = "Assets";

		if (!Directory.Exists(path))
			path = Path.GetDirectoryName(path);

		var proxyExtras = ScriptableObject.CreateInstance<ProxyExtras>();
		path = AssetDatabase.GenerateUniqueAssetPath(path + "/ProxyExtras.asset");
		AssetDatabase.CreateAsset(proxyExtras, path);
	}
#endif

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
