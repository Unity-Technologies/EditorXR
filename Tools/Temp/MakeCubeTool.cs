using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Tools;
using UnityEngine.VR;

[MainMenuItem("Cube", "Create", "Create cubes in the scene")]
public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin, IActions
{
	class CubeToolAction : IAction
	{
		public Sprite icon { get; internal set; }
		public void ExecuteAction()
		{
		}
	}

	[SerializeField]
	Sprite m_Icon;

	readonly CubeToolAction m_CubeToolAction = new CubeToolAction();

	public List<IAction> actions { get; private set; }
	public Transform rayOrigin { get; set; }
	public Standard standardInput { get; set; }
	public Node selfNode { get; set; }

	void Awake()
	{
		m_CubeToolAction.icon = m_Icon;
		actions = new List<IAction>() { m_CubeToolAction };
	}

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
