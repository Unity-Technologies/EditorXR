using UnityEngine;
using UnityEngine.UI;

public class ClipText : Text
{
	public Matrix4x4 parentMatrix { private get; set; }
	public Vector3 clipExtents { private get; set; }

	public override Material GetModifiedMaterial(Material baseMaterial)
	{
		var material = base.GetModifiedMaterial(baseMaterial);
		material.SetMatrix("_ParentMatrix", parentMatrix);
		material.SetVector("_ClipExtents", clipExtents);
		return material;
	}
}