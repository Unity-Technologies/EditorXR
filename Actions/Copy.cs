using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Copy", ActionMenuItemAttribute.kDefaultActionSectionName, 5)]
	public class Copy : BaseAction
	{
		public override void ExecuteAction()
		{
			//bug (case 451825)
			//http://forum.unity3d.com/threads/editorapplication-ExecuteActionmenuitem-dont-include-edit-menu.148215/
			//return EditorApplication.ExecuteActionMenuItem("Edit/Copy");

			var selection = Selection.activeObject;
			Paste.buffer = selection;
		}
	}
}