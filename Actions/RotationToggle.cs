using System;
using UnityEditor;

namespace UnityEngine.VR.Actions
{
	//[ToggleActionMenuItem("Rotation", "Local",  "Assets/EditorVR/Actions/Icons/RotationLocalIcon", "Global",  "Assets/EditorVR/Actions/Icons/RotationGlobalIcon", ActionMenuItemAttribute.kDefaultActionSectionName, 9)]
	public class RotationToggle : MonoBehaviour, IUsesTransformTool
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

		public bool ExecuteAction()
		{
			Debug.LogError("Toggle Local/Global rotation Action called!");

			//SwitchPivotRotation in transform tool called here

			return false;
		}

	}
}