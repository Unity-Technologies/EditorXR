#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HighlightModule : MonoBehaviour, IUsesGameObjectLocking
	{
		[SerializeField]
		Material m_DefaultHighlightMaterial;

		[SerializeField]
		Material m_LeftHighlightMaterial;

		[SerializeField]
		Material m_RightHighlightMaterial;

		readonly Dictionary<Material, HashSet<GameObject>> m_Highlights = new Dictionary<Material, HashSet<GameObject>>();
		readonly Dictionary<Node, HashSet<Transform>> m_NodeMap = new Dictionary<Node, HashSet<Transform>>();

		public event Func<GameObject, Material, bool> customHighlight
		{
			add { m_CustomHighlightFuncs.Add(value); }
			remove { m_CustomHighlightFuncs.Remove(value); }
		}
		readonly List<Func<GameObject, Material, bool>> m_CustomHighlightFuncs = new List<Func<GameObject, Material, bool>>();

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

					var shouldHighlight = true;
					for (int i = 0; i < m_CustomHighlightFuncs.Count; i++)
					{
						var func = m_CustomHighlightFuncs[i];
						if (func(go, material))
							shouldHighlight = false;
					}

					if (shouldHighlight)
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

		public void SetHighlight(GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false)
		{
			if (go == null)
				return;

			if (!force && active && this.IsLocked(go))
				return;

			if (material == null)
			{
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

					material = node == Node.LeftHand ? m_LeftHighlightMaterial : m_RightHighlightMaterial;
				}
				else
				{
					material = m_DefaultHighlightMaterial;
				}
			}

			if (active) // Highlight
			{
				// Do not highlight if the selection contains this object or any of its parents
				if (!force && Selection.transforms.Any(selection => go.transform == selection || go.transform.IsChildOf(selection)))
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
				if (force)
				{
					// A force removal removes the GameObject regardless of how it was highlighted (e.g. with a specific hand)
					foreach (var gameObjects in m_Highlights.Values)
					{
						gameObjects.Remove(go);
					}
				}
				else
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
}
#endif
