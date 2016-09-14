using System;
using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ToggleActionItem("Rotation", "Local",  "ActionIcons/RotationLocalIcon", "Global",  "ActionIcons/RotationGlobalIcon", "DefaultActions", 5)]
	[ExecuteInEditMode]
	public class RotationToggle : MonoBehaviour, IToggleAction, IUsesTransformTool
	{
		[SerializeField]
		private Sprite m_Icon01;
		
		[SerializeField]
		private Sprite m_Icon02;

		public string groupName { get; set; }
		public string sectionName { get; set; }
		public int indexPosition { get; set; }
		public Sprite icon { get; set; }
		public Sprite icon02 { get; set; }

		public Func<PivotMode> switchOriginMode { get; set; }
		public Func<PivotRotation> switchRotationMode { get; set; }

		public bool Execute()
		{
			Debug.LogError("Toggle Local/Global rotation Action called!");

			//SwitchPivotRotation in transform tool called here

			return false;
		}

	}
}