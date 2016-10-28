using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Tools;
using UnityEngine.VR;
using Object = UnityEngine.Object;

//[MainMenuItem("Cube", "Create", "Create cubes in the scene")]
[MainMenuItem(false)]
public class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IToolActions, ISpatialHash
{
	class CubeToolAction : IAction
	{
		public Sprite icon { get; internal set; }
		public bool ExecuteAction()
		{
			return true;
		}
	}

	[SerializeField]
	Sprite m_Icon;

	readonly CubeToolAction m_CubeToolAction = new CubeToolAction();

	public List<IAction> toolActions { get; private set; }
	public Transform rayOrigin { get; set; }
	public Standard standardInput { get; set; }
	public Node selfNode { get; set; }

	public Action<Object> addObjectToSpatialHash { get; set; }
	public Action<Object> removeObjectFromSpatialHash { get; set; }

	void Awake()
	{
		m_CubeToolAction.icon = m_Icon;
		toolActions = new List<IAction>() { m_CubeToolAction };
	}

	private void Update()
	{
		if (standardInput.action.wasJustPressed)
		{
			Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			if (rayOrigin)
				cube.position = rayOrigin.position + rayOrigin.forward * 5f;

			addObjectToSpatialHash(cube);
		}
	}
}