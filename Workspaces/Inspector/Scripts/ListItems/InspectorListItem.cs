using ListView;
using UnityEngine;
using UnityEngine.VR.Handles;

public class InspectorListItem : ListViewItem<InspectorData>
{
	[SerializeField]
	private BaseHandle m_Cube;

	public void SwapMaterials(Material cubeMaterial)
	{
		m_Cube.GetComponent<Renderer>().sharedMaterial = cubeMaterial;
	}

	public void GetMaterials(out Material cubeMaterial)
	{
		cubeMaterial = Instantiate(m_Cube.GetComponent<Renderer>().sharedMaterial);
	}

	public void UpdateTransforms(float width, int depth)
	{
		Vector3 cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;
	}
}