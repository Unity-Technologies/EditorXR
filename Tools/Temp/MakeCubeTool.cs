using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Tools;

//[MainMenuItem("Cube", "Create", "Create cubes in the scene")]
[MainMenuItem(false)]
public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin, IActions, IUsesSpatialHash
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

	public Action<GameObject> addToSpatialHash { get; set; }
	public Action<GameObject> removeFromSpatialHash { get; set; }

	void Awake()
	{
		m_CubeToolAction.icon = m_Icon;
		actions = new List<IAction>() { m_CubeToolAction };
	}

	public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
	{
		var standardInput = (Standard)input;
		if (standardInput.action.wasJustPressed)
		{
			var cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			if (rayOrigin)
				cube.position = rayOrigin.position + rayOrigin.forward * 5f;

			addToSpatialHash(cube.gameObject);

			consumeControl(standardInput.action);
		}
	}
}
