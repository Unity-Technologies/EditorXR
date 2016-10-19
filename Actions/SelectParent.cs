using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("SelectParent", ActionMenuItemAttribute.kDefaultActionSectionName, 8)]
	public class SelectParent : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public bool ExecuteAction()
		{
			var go = Selection.activeGameObject;
			if (go != null)
			{
				var parent = go.transform.parent;
				if (parent != null)
				{
					var parentGO = parent.gameObject;
					if (parentGO)
					{
						Selection.activeGameObject = parentGO;
						return true;
					}
				}
			}

			return false;
		}
	}
}
