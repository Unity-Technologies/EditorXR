using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IRay
{	
	public Standard standardInput
	{
		get; set;
	}

	public Transform rayOrigin { get; set; }
	
	private void Update()
	{
		if (standardInput.action.wasJustPressed)
		{
			Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			if (rayOrigin)
			{
				cube.position = rayOrigin.position + rayOrigin.forward * 5f;
				cube.parent = transform;
			}
		}

	}

}
