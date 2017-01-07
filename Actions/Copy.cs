using UnityEditor;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Copy", ActionMenuItemAttribute.kDefaultActionSectionName, 5)]
	public class Copy : BaseAction
	{
		public override void ExecuteAction()
		{
			//bug (case 451825)
			//http://forum.unity3d.com/threads/editorapplication-ExecuteActionmenuitem-dont-include-edit-menu.148215/
			//return EditorApplication.ExecuteActionMenuItem("Edit/Copy");

			Paste.buffer = Selection.gameObjects;
		}
	}
}