using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IRay
{	
	public Standard StandardInput
	{
		get; set;
	}

	public Transform RayOrigin { get; set; }
	
	private void Update()
	{
		if (StandardInput.action.wasJustPressed)
		{
			Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			if (RayOrigin)
			{
				cube.position = RayOrigin.position + RayOrigin.forward * 5f;
            }
		}

	}

}
