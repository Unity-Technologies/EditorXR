using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[ActionItemAttribute("Paste", "ActionIcons/PasteIcon")]
	[ExecuteInEditMode]
	public class Paste : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should paste content here");
			//return EditorApplication.ExecuteMenuItem("Edit/Paste");

			var selection = Copy.selectionCopy;
			if (selection != null)
			{
				selection = Object.Instantiate(selection.gameObject);

				if (selection != null)
				{
					selection.hideFlags = HideFlags.None;
					foreach (var child in selection.GetComponentsInChildren<Transform>())
						child.hideFlags = HideFlags.None;
				}

				return true;
			}
			else
				return false;
		}
	}
}