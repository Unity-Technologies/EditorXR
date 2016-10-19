namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Paste", "Assets/EditorVR/Actions/Icons/PasteIcon.png", ActionMenuItemAttribute.kDefaultActionSectionName, 6)]
	public class Paste : MonoBehaviour, IAction
	{
		public static Object buffer { get; set; }

		public bool ExecuteAction()
		{
			//return EditorApplication.ExecuteActionMenuItem("Edit/Paste");

			if (buffer != null)
			{
				Instantiate(buffer);
				return true;
			}

			return false;
		}
	}
}