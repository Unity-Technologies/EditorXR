#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	[MainMenuItem("XTool4", "Create", "4 Extra Demo Tool")]
	//[MainMenuItem(false)]
	sealed class ToolFourDemo : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, IUsesSpatialHash
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
		}
	}
}
#endif
