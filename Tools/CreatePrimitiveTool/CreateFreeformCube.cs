using UnityEngine;
using UnityEngine.VR.Tools;
using System.Collections;

[MainMenuItem("Freeform","Primitive","Draw 3D rectangles of any dimension")]
public class CreateFreeformCube : CreatePrimitiveTool
{
	protected override void Awake()
	{
		base.Awake();
		s_SelectedPrimitiveType = PrimitiveType.Cube;
		s_Freeform = true;
	}
}