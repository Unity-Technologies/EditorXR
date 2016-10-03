using UnityEngine;
using UnityEngine.VR.Tools;
using System.Collections;

[MainMenuItem("Sphere","Primitive","")]
public class CreateSphere : CreatePrimitiveTool
{
	protected override void Awake()
	{
		base.Awake();
		s_SelectedPrimitiveType = PrimitiveType.Sphere;
		s_Freeform = false;
	}
}