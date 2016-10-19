using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Delete", ActionMenuItemAttribute.kDefaultActionSectionName, 7)]
	public class Delete : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public bool ExecuteAction()
		{
			var selection = Selection.activeObject;
			if (selection)
			{
				U.Object.Destroy(selection);
				return true;
			}

			return false;
		}
	}
}