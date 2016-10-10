using System;
using UnityEngine.VR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Tools;

public class CreatePrimitiveMenu : MonoBehaviour
{
	[SerializeField]
	GameObject HighlightCube;
	[SerializeField]
	GameObject HighlightSphere;
	[SerializeField]
	GameObject HighlightCapsule;
	[SerializeField]
	GameObject HighlightPlane;
	[SerializeField]
	GameObject HighlightQuad;
	[SerializeField]
	GameObject HighlightCylinder;

	public void CreatePrimitive(int type)
	{
		CreatePrimitiveTool.s_SelectedPrimitiveType = (PrimitiveType)type;
		CreatePrimitiveTool.s_Freeform = false;

		UpdateCurrentHightlightObject();
    }

	void UpdateCurrentHightlightObject()
	{
		HighlightCube.SetActive(false);
		HighlightSphere.SetActive(false);
		HighlightCapsule.SetActive(false);
		HighlightPlane.SetActive(false);
		HighlightQuad.SetActive(false);
		HighlightCylinder.SetActive(false);

		switch(CreatePrimitiveTool.s_SelectedPrimitiveType)
		{
			case PrimitiveType.Cube:
			{
				HighlightCube.SetActive(true);
				break;
			}
			case PrimitiveType.Sphere:
			{
				HighlightSphere.SetActive(true);
				break;
			}
			case PrimitiveType.Capsule:
			{
				HighlightCapsule.SetActive(true);
				break;
			}
			case PrimitiveType.Plane:
			{
				HighlightPlane.SetActive(true);
				break;
			}
			case PrimitiveType.Quad:
			{
				HighlightQuad.SetActive(true);
				break;
			}
			case PrimitiveType.Cylinder:
			{
				HighlightCylinder.SetActive(true);
				break;
			}
		}
	}

	public void CreateFreeformCube()
	{
		CreatePrimitiveTool.s_SelectedPrimitiveType = PrimitiveType.Cube;
		CreatePrimitiveTool.s_Freeform = true;
    }
}
