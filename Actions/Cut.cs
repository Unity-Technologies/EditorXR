using UnityEditor;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Cut", ActionMenuItemAttribute.kDefaultActionSectionName, 4)]
	public class Cut : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		Sprite m_Icon;

		public bool ExecuteAction()
		{
			//bug (case 451825)
			//http://forum.unity3d.com/threads/editorapplication-ExecuteActionmenuitem-dont-include-edit-menu.148215/
			//return EditorApplication.ExecuteActionMenuItem("Edit/Cut");

			var selection = Selection.activeObject;
			if (selection != null)
			{
				selection.hideFlags = HideFlags.HideAndDontSave;
				var go = selection as GameObject;
				if (go)
					go.SetActive(false);
				Paste.buffer = selection;
				Selection.activeObject = null;
				return true;
			}

			return false;
		}
	}
}