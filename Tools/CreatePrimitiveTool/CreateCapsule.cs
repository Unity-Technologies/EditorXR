using UnityEngine;
using UnityEngine.VR.Tools;
using System.Collections;

[MainMenuItem("Capsule","Primitive","")]
public class CreateCapsule : CreatePrimitiveTool
{
	protected override void Awake()
	{
		base.Awake();
		s_SelectedPrimitiveType = PrimitiveType.Capsule;
		s_Freeform = false;
	}
}