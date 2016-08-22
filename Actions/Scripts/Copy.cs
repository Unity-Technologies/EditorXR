using UnityEditor;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[VRMenuItem("Copy", "Actions", "Copy selected object")]
	[ExecuteInEditMode]
	public class Copy : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		private static GameObject m_SelectionCopy;

		public Sprite icon { get; set; }
		public static GameObject selectionCopy { get { return m_SelectionCopy; } }

		public bool Execute()
		{
			Debug.LogError("Execute Action should Copy content here");
			//bug (case 451825)
			//http://forum.unity3d.com/threads/editorapplication-executemenuitem-dont-include-edit-menu.148215/
			//return EditorApplication.ExecuteMenuItem("Edit/Copy");
			var selection = UnityEditor.Selection.GetTransforms(SelectionMode.Deep);

			if (selection != null)
			{
				if (m_SelectionCopy != null)
					DestroyImmediate(m_SelectionCopy);

				m_SelectionCopy = Object.Instantiate(selection[0].gameObject);

				if (m_SelectionCopy != null)
				{
					m_SelectionCopy.hideFlags = HideFlags.HideAndDontSave;
					foreach (var child in m_SelectionCopy.GetComponentsInChildren<Transform>())
						child.hideFlags = HideFlags.HideAndDontSave;
				}
			}

			return true;
		}
	}
}