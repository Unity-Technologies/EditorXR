using System;
using UnityEngine.VR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Tools;

public class CreatePrimitiveMenu : MonoBehaviour
{
	[SerializeField]
	public GameObject Cube;
	[SerializeField]
	public GameObject Sphere;

	public void CreatePrimitive(int type)
	{
		CreatePrimitiveTool.s_SelectedPrimitiveType = (PrimitiveType)type;
		CreatePrimitiveTool.s_Freeform = false;
	}

	public void CreateFreeformCube()
	{
		CreatePrimitiveTool.s_SelectedPrimitiveType = PrimitiveType.Cube;
		CreatePrimitiveTool.s_Freeform = true;
    }
}