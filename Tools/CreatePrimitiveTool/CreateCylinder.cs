using UnityEngine;
using UnityEngine.VR.Tools;
using System.Collections;

[MainMenuItem("Cylinder","Primitive","")]
public class CreateCylinder : CreatePrimitiveTool
{
	protected override void Awake()
	{
		base.Awake();
		s_SelectedPrimitiveType = PrimitiveType.Cylinder;
		s_Freeform = false;
	}
}