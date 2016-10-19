using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Delete", "Assets/EditorVR/Actions/Icons/DeleteIcon.png", ActionMenuItemAttribute.kDefaultActionSectionName, 7)]
	public class Delete : MonoBehaviour, IAction
	{
		public bool ExecuteAction()
		{
			var selection = Selection.activeObject;
			if (selection)
			{
				U.Object.Destroy(selection);
				return true;
			}

			return false;
		}
	}
}