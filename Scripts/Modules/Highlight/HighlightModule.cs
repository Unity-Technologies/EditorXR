using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class HighlightModule : MonoBehaviour
{
	[SerializeField]
	private Material m_HighlightMaterial;

	private readonly Dictionary<GameObject, int> m_HighlightCounts = new Dictionary<GameObject, int>();

	void LateUpdate()
	{
		foreach (var go in m_HighlightCounts.Keys)
		{
			if (go == null)
				continue;

			var highlightTransform = go.transform;
			var highlightScaleIncrease = Vector3.one * 0.0125f;
			Matrix4x4 highlightScaleIncreaseMatrix = Matrix4x4.TRS(highlightTransform.position, highlightTransform.rotation, highlightTransform.lossyScale + highlightScaleIncrease);
			foreach (var m in go.GetComponentsInChildren<MeshFilter>())
			{
				for (var i = 0; i < m.sharedMesh.subMeshCount; i++)
					Graphics.DrawMesh(m.sharedMesh, highlightScaleIncreaseMatrix, m_HighlightMaterial, m.gameObject.layer, null, i);
			}
		}
	}

	public void SetHighlight(GameObject go, bool active)
	{
		if (go == null || go.isStatic)
			return;

		if (active) // Highlight
		{
			if (Selection.gameObjects.Contains(go))
				return;

			if (!m_HighlightCounts.ContainsKey(go))
				m_HighlightCounts.Add(go, 1);
			else
				m_HighlightCounts[go]++;
		}
		else // Unhighlight
		{
			int count;
			if(m_HighlightCounts.TryGetValue(go, out count))
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
