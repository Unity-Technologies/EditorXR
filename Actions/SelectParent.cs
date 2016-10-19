using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("SelectParent", "Assets/EditorVR/Actions/Icons/SelectParentIcon.png", ActionMenuItemAttribute.kDefaultActionSectionName, 8)]
	public class SelectParent : MonoBehaviour, IAction
	{
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
