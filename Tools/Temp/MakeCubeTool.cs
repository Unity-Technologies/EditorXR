using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IRay
{	
	
	[SerializeField]
	private GameObject m_TestPrefab;

	public Standard standardInput
	{
		get; set;
	}

	public Transform rayOrigin { get; set; }
	
	private void Update()
	{
		if (standardInput.action.wasJustPressed)
		{
			Transform cube = U.Object.InstantiateAndSetActive(m_TestPrefab).transform;
			if (rayOrigin)
			{
				cube.position = rayOrigin.position + rayOrigin.forward * 5f;
				cube.parent = transform;
			}
		}

	}

}
