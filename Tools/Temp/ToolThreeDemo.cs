#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	[MainMenuItem("XTool3", "Create", "3 Extra Demo Tool")]
	//[MainMenuItem(false)]
	sealed class ToolThreeDemo : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, IUsesSpatialHash
	{
		public Transform rayOrigin { get; set; }

		public ActionMap actionMap
		{
			get { return m_ActionMap; }
			set { m_ActionMap = value; }
		}

		[SerializeField]
		private ActionMap m_ActionMap;

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var standardAlt = (StandardAlt)input;
			if (standardAlt.action.wasJustPressed)
			{
				Transform sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
				if (rayOrigin)
					sphere.position = rayOrigin.position + rayOrigin.forward * 5f;

				this.AddToSpatialHash(sphere.gameObject);

				consumeControl(standardAlt.action);
			}
		}
	}
}
#endif
