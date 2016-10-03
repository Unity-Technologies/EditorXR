using UnityEngine;
using UnityEngine.VR.Tools;
using System.Collections;

[MainMenuItem("Cube","Primitive","")]
public class CreateCube : CreatePrimitiveTool
{
	protected override void Awake()
	{
		base.Awake();
		s_SelectedPrimitiveType = PrimitiveType.Cube;
		s_Freeform = false;
	}
}