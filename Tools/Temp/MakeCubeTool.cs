using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

[MainMenuItem("Cube", "Create", "Create cubes in the scene")]
public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IRay
{	
	public Transform rayOrigin { get; set; }
	public Standard standardInput { get; set; }
    
	private void Update()
	{
		if (standardInput.action.wasJustPressed)
		{
			Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			if (rayOrigin)
				cube.position = rayOrigin.position + rayOrigin.forward * 5f;
		}
	}

}
