using UnityEngine.VR.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class CreatePrimitiveMenu : MonoBehaviour
{
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
