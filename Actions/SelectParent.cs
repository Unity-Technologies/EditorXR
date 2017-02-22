using UnityEditor;

namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("SelectParent", ActionMenuItemAttribute.kDefaultActionSectionName, 8)]
	public class SelectParent : BaseAction
	{
		public override void ExecuteAction()
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
					}
				}
			}
		}
	}
}
