#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	[MainMenuItem("Cube", "Create", "Create cubes in the scene")]
	//[MainMenuItem(false)]
	sealed class MakeCubeTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin, IUsesSpatialHash, IMenuIcon
	{
		[SerializeField]
		Sprite m_Icon;

		public Transform rayOrigin { get; set; }
		public Sprite icon { get { return m_Icon; } }

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
