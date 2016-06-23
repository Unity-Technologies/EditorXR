using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap
{	
	public Standard StandardInput
	{
		get; set;
	}
	
	private void Update()
	{
		if (StandardInput.action.wasJustPressed)
		{
			GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Transform cubeTransform = cube.transform;
		}

	}
}
