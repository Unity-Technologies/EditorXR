namespace UnityEngine.VR.Actions
{
	[ActionItem("Paste", "ActionIcons/PasteIcon", "DefaultActions", 6)]
	[ExecuteInEditMode]
	public class Paste : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;
		public Sprite icon { get; set; }

		public int indexPosition { get; set; }
		public string sectionName { get; set; }

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