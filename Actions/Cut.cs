using UnityEditor;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Cut", ActionMenuItemAttribute.kDefaultActionSectionName, 4)]
	public class Cut : BaseAction
	{
		public override void ExecuteAction()
		{
			//bug (case 451825)
			//http://forum.unity3d.com/threads/editorapplication-ExecuteActionmenuitem-dont-include-edit-menu.148215/
			//return EditorApplication.ExecuteActionMenuItem("Edit/Cut");

			var selection = Selection.gameObjects;
			if (selection != null)
			{
				foreach (var go in selection)
				{
					go.hideFlags = HideFlags.HideAndDontSave;
					go.SetActive(false);
				}

				Paste.buffer = selection;
				Selection.activeGameObject = null;
			}
		}
	}
}