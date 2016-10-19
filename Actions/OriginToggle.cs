using System;
using UnityEditor;
using UnityEngine.VR.Menus;

namespace UnityEngine.VR.Actions
{
	//[ToggleActionMenuItem("Rotation", "Center",  "Assets/EditorVR/Actions/Icons/OriginCenterIcon", "Pivot",  "Assets/EditorVR/Actions/Icons/OriginPivotIcon", ActionMenuItemAttribute.kDefaultActionSectionName, 10)]
	public class OriginToggle : MonoBehaviour, IUsesTransformTool
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
			Debug.LogError("Toggle Origin Center/Pivot Action called!");

			//SwitchPivotMode in transform tool called here

			return false;
		}
	}
}