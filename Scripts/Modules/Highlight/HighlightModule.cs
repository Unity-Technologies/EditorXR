#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HighlightModule : MonoBehaviour
	{
		[SerializeField]
		Material m_LeftHighlightMaterial;

		[SerializeField]
		Material m_RightHighlightMaterial;

		readonly Dictionary<Material, HashSet<GameObject>> m_Highlights = new Dictionary<Material, HashSet<GameObject>>();
		readonly Dictionary<Node, HashSet<Transform>> m_NodeMap = new Dictionary<Node, HashSet<Transform>>();

		public Color leftColor
		{
			get { return m_LeftHighlightMaterial.color; }
		}

		public Color rightColor
		{
			get { return m_RightHighlightMaterial.color; }
		}

		void LateUpdate()
		{
			foreach (var highlight in m_Highlights)
			{
				var material = highlight.Key;
				var highlights = highlight.Value;
				foreach (var go in highlights)
				{
					if (go == null)
						continue;

					HighlightObject(go, material);
				}
			}
		}

		static void HighlightObject(GameObject go, Material material)
		{
			foreach (var m in go.GetComponentsInChildren<MeshFilter>())
			{
				if (m.sharedMesh == null)
					continue;

				for (var i = 0; i < m.sharedMesh.subMeshCount; i++)
					Graphics.DrawMesh(m.sharedMesh, m.transform.localToWorldMatrix, material, m.gameObject.layer, null, i);
			}
		}

		public void AddRayOriginForNode(Node node, Transform rayOrigin)
		{
			HashSet<Transform> set;
			if (!m_NodeMap.TryGetValue(node, out set))
			{
				set = new HashSet<Transform>();
				m_NodeMap[node] = set;
			}

			set.Add(rayOrigin);
		}

		public void SetHighlight(GameObject go, bool active, Transform rayOrigin = null, Material material = null)
		{
			if (go == null || go.isStatic)
				return;

			if (rayOrigin == null && material == null)
			{
				Debug.LogError("You must specify a rayOrigin or material in order to set highlight");
				return;
			}

			if (rayOrigin)
			{
				var node = Node.LeftHand;
				foreach (var kvp in m_NodeMap)
				{
					if (kvp.Value.Contains(rayOrigin))
					{
						node = kvp.Key;
						break;
					}
				}

				// rayOrigin takes precedent over material
				material = node == Node.LeftHand ? m_LeftHighlightMaterial : m_RightHighlightMaterial;
			}

			if (active) // Highlight
			{
				// Do not highlight if the selection contains this object or any of its parents
				if (Selection.transforms.Any(selection => go.transform == selection || go.transform.IsChildOf(selection)))
					return;

				HashSet<GameObject> gameObjects;
				if (!m_Highlights.TryGetValue(material, out gameObjects))
				{
					gameObjects = new HashSet<GameObject>();
					m_Highlights[material] = gameObjects;
				}
				gameObjects.Add(go);
			}
			else // Unhighlight
			{
				HashSet<GameObject> gameObjects;
				if (m_Highlights.TryGetValue(material, out gameObjects))
				{
					gameObjects.Remove(go);
				}
			}
		}
	}
}
#endif
