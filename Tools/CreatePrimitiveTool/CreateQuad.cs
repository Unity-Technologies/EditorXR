using UnityEngine;
using UnityEngine.VR.Tools;
using System.Collections;

//[MainMenuItem("Quad","Primitive","")]
public class CreateQuad : CreatePrimitiveTool
{
	protected override void Awake()
	{
		base.Awake();
		s_SelectedPrimitiveType = PrimitiveType.Quad;
		s_Freeform = false;
	}
}