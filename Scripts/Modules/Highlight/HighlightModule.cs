#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HighlightModule : MonoBehaviour
	{
		[SerializeField]
		Material m_DefaultHighlightMaterial;

		[SerializeField]
		Material m_LeftHighlightMaterial;

		[SerializeField]
		Material m_RightHighlightMaterial;

		readonly Dictionary<Transform, HashSet<GameObject>> m_Highlights = new Dictionary<Transform, HashSet<GameObject>>();
		readonly HashSet<GameObject> m_DefaultHighlights = new HashSet<GameObject>();
		readonly Dictionary<Node, HashSet<Transform>> m_NodeMap = new Dictionary<Node, HashSet<Transform>>();

		void LateUpdate()
		{
			foreach (var highlight in m_Highlights)
			{
				var highlights = highlight.Value;
				foreach (var go in highlights)
				{
					if (go == null)
						continue;

					var node = Node.LeftHand;
					foreach (var kvp in m_NodeMap)
					{
						if (kvp.Value.Contains(highlight.Key))
						{
							node = kvp.Key;
							break;
						}
					}

					var material = node == Node.LeftHand ? m_LeftHighlightMaterial : m_RightHighlightMaterial;

					HighlightObject(go, material);
				}
			}

			foreach (var go in m_DefaultHighlights)
			{
				HighlightObject(go, m_DefaultHighlightMaterial);
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

		public void SetHighlight(GameObject go, Transform rayOrigin, bool active)
		{
			if (go == null || go.isStatic)
				return;

			if (active) // Highlight
			{
				// Do not highlight if the selection contains this object or any of its parents
				if (Selection.transforms.Any(selection => go.transform == selection || go.transform.IsChildOf(selection)))
					return;

				if (rayOrigin == null)
				{
					m_DefaultHighlights.Add(go);
					return;
				}

				HashSet<GameObject> gameObjects;
				if (!m_Highlights.TryGetValue(rayOrigin, out gameObjects))
				{
					gameObjects = new HashSet<GameObject>();
					m_Highlights[rayOrigin] = gameObjects;
				}
				gameObjects.Add(go);
			}
			else // Unhighlight
			{
				if (rayOrigin == null)
				{
					m_DefaultHighlights.Remove(go);
					return;
				}

				HashSet<GameObject> gameObjects;
				if (m_Highlights.TryGetValue(rayOrigin, out gameObjects))
				{
					gameObjects.Remove(go);
				}
			}
		}
	}
}
#endif
