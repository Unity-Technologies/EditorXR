using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Paste", ActionMenuItemAttribute.kDefaultActionSectionName, 6)]
	public class Paste : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public static Object buffer { get; set; }

		public bool ExecuteAction()
		{
			//return EditorApplication.ExecuteActionMenuItem("Edit/Paste");

			if (buffer != null)
			{
				var gameObject = buffer as GameObject;
				Object pasted;
				if (gameObject)
				{
					pasted = U.Object.Instantiate(gameObject);
					((GameObject)pasted).SetActive(true);
				}
				else
				{
					pasted = Instantiate(buffer);
				}

				pasted.hideFlags = HideFlags.None;
				return true;
			}

			return false;
		}
	}
}