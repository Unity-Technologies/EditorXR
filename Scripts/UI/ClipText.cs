using UnityEngine;
using UnityEngine.UI;

public class ClipText : Text
{
	public Matrix4x4 parentMatrix { private get; set; }
	public Vector3 clipExtents { private get; set; }

	private Material m_ModifiedMaterial;

	public void UpdateMaterialClip()
	{
		if (m_ModifiedMaterial != null)
		{
			m_ModifiedMaterial.SetMatrix("_ParentMatrix", parentMatrix);
			m_ModifiedMaterial.SetVector("_ClipExtents", clipExtents);
		}
	}

	public override Material GetModifiedMaterial(Material baseMaterial)
	{
		m_ModifiedMaterial = base.GetModifiedMaterial(baseMaterial);
		UpdateMaterialClip();
		return m_ModifiedMaterial;
	}
}