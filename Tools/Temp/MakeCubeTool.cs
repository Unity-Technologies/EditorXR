#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	[MainMenuItem("Cube", "Create", "Create cubes in the scene")]
	//[MainMenuItem(false)]
	sealed class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin, IUsesSpatialHash, IMenuIcon
	{
		class CubeToolAction : IAction
		{
			public Sprite icon { get; internal set; }
			public void ExecuteAction() {}
		}

		[SerializeField]
		Sprite m_Icon;

		readonly CubeToolAction m_CubeToolAction = new CubeToolAction();

		public List<IAction> actions { get; private set; }
		public Transform rayOrigin { get; set; }
		public Sprite icon { get { return m_Icon; } }

		void Awake()
		{
			m_CubeToolAction.icon = m_Icon;
			actions = new List<IAction>() { m_CubeToolAction };
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var standardInput = (Standard)input;
			if (standardInput.action.wasJustPressed)
			{
				var cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
				if (rayOrigin)
					cube.position = rayOrigin.position + rayOrigin.forward * 5f;

				this.AddToSpatialHash(cube.gameObject);

				consumeControl(standardInput.action);
			}
		}
	}
}
#endif
