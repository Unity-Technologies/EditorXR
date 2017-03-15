#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HighlightModule : MonoBehaviour, IUsesGameObjectLocking
	{
		static readonly Vector3 k_HighlightScaleIncrease = Vector3.one * 0.0125f;

		[SerializeField]
		private Material m_HighlightMaterial;

		private readonly Dictionary<GameObject, int> m_HighlightCounts = new Dictionary<GameObject, int>();

		public Action<GameObject, bool> setLocked { private get; set; }
		public Func<GameObject, bool> isLocked { private get; set; }

		void LateUpdate()
		{
			foreach (var go in m_HighlightCounts.Keys)
			{
				if (go == null)
					continue;

				foreach (var m in go.GetComponentsInChildren<MeshFilter>())
				{
					var highlightTransform = m.transform;
					Matrix4x4 highlightScaleIncreaseMatrix = Matrix4x4.TRS(highlightTransform.position, highlightTransform.rotation, highlightTransform.lossyScale + k_HighlightScaleIncrease);

					if (m.sharedMesh == null)
						continue;

					for (var i = 0; i < m.sharedMesh.subMeshCount; i++)
						Graphics.DrawMesh(m.sharedMesh, highlightScaleIncreaseMatrix, m_HighlightMaterial, m.gameObject.layer, null, i);
				}
			}
		}

		public void SetHighlight(GameObject go, bool active)
		{
			if (go == null || isLocked(go))
				return;

			if (active) // Highlight
			{
				// Do not highlight if the selection contains this object or any of its parents
				if (Selection.transforms.Any(selection => go.transform == selection || go.transform.IsChildOf(selection)))
					return;

				if (!m_HighlightCounts.ContainsKey(go))
					m_HighlightCounts.Add(go, 1);
				else
					m_HighlightCounts[go]++;
			}
			else // Unhighlight
			{
				int count;
				if (m_HighlightCounts.TryGetValue(go, out count))
				{
					count--;
					if (count <= 0)
						m_HighlightCounts.Remove(go);
					else
						m_HighlightCounts[go] = count;
				}
			}
		}
	}
}
#endif
