using UnityEngine;
using UnityEngine.UI;

public class ClipText : Text
{
	public static Matrix4x4 parentMatrix;
	public static Vector3 clipExtents;

	public override Material GetModifiedMaterial(Material baseMaterial)
	{
		var material = base.GetModifiedMaterial(baseMaterial);
		material.SetMatrix("_ParentMatrix", parentMatrix);
		material.SetVector("_ClipExtents", clipExtents);
		return material;
	}
}