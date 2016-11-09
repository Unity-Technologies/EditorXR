using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Paste", ActionMenuItemAttribute.kDefaultActionSectionName, 6)]
	public class Paste : BaseAction
	{
		public static Object buffer { get; set; }

		public override void ExecuteAction()
		{
			//return EditorApplication.ExecuteActionMenuItem("Edit/Paste");

			if (buffer != null)
			{
				var pasted = Instantiate(buffer);
				pasted.hideFlags = HideFlags.None;
				var go = pasted as GameObject;
				if (go)
					go.SetActive(true);
			}
		}
	}
}