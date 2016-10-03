using UnityEngine;
using UnityEngine.VR.Tools;
using System.Collections;

[MainMenuItem("Plane","Primitive","")]
public class CreatePlane : CreatePrimitiveTool
{
	protected override void Awake()
	{
		base.Awake();
		s_SelectedPrimitiveType = PrimitiveType.Plane;
		s_Freeform = false;
	}
}