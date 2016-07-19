using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HighlightModule : MonoBehaviour
{
	[SerializeField]
	private Material m_HighlightMaterial;

	private readonly Dictionary<GameObject, int> m_HighlightCounts = new Dictionary<GameObject, int>();


	void Update()
	{
		foreach (var go in m_HighlightCounts.Keys)
		{
			if (go == null)
				continue;
			foreach (var m in go.GetComponentsInChildren<MeshFilter>())
			{
				for (int i = 0; i < m.sharedMesh.subMeshCount; i++)
					Graphics.DrawMesh(m.sharedMesh, m.transform.localToWorldMatrix, m_HighlightMaterial, 0, null, i);
			}
		}
	}

	public void SetHighlight(GameObject go, bool active)
	{
		if (go == null)
			return;

		if (active) // Highlight
		{
			if (!m_HighlightCounts.ContainsKey(go))
				m_HighlightCounts.Add(go, 1);
			else
				m_HighlightCounts[go]++;
		}
		else // Unhighlight
		{
			if (!m_HighlightCounts.ContainsKey(go))
			{
				Debug.LogError("Unhighlight called on object that is not currently highlighted: " + go);
				return;
			}
			else
				m_HighlightCounts[go]--;

			if (m_HighlightCounts[go] == 0)
			{
				m_HighlightCounts.Remove(go);
			}
		}
	}
}
