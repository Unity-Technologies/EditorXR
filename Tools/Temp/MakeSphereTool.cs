#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	[MainMenuItem("Sphere", "Create", "Create spheres in the scene")]
	//[MainMenuItem(false)]
	sealed class MakeSphereTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin, IUsesSpatialHash, IMenuIcon
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
				Transform sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
				if (rayOrigin)
					sphere.position = rayOrigin.position + rayOrigin.forward * 5f;

				this.AddToSpatialHash(sphere.gameObject);

				consumeControl(standardInput.action);
			}
		}
	}
}
#endif
