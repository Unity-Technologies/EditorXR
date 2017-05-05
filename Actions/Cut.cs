#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Cut", ActionMenuItemAttribute.DefaultActionSectionName, 4)]
	sealed class Cut : BaseAction
	{
		public override void ExecuteAction()
		{
			//bug (case 451825)
			//http://forum.unity3d.com/threads/editorapplication-ExecuteActionmenuitem-dont-include-edit-menu.148215/
			//return EditorApplication.ExecuteActionMenuItem("Edit/Cut");

			var selection = Selection.transforms;
			if (selection != null)
			{
				foreach (var transform in selection)
				{
					var go = transform.gameObject;
					go.hideFlags = HideFlags.HideAndDontSave;
					go.SetActive(false);
				}

				Paste.buffer = selection;
				Selection.activeGameObject = null;
			}
		}
	}
}
#endif
