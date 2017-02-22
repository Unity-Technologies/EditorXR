namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Copy", ActionMenuItemAttribute.DefaultActionSectionName, 5)]
	internal sealed class Copy : BaseAction
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