using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ActionItem("SelectParent", "ActionIcons/SelectParentIcon", "DefaultActions", 8)]
	public class SelectParent : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public int indexPosition { get; set; }
		public string sectionName { get; set; }
		public Sprite icon { get; set; }
		
		public bool Execute()
		{
			Debug.LogError("<color=yellow>Attempting to Select Parent of currently selected object</color>");
			bool successfull = false;

			var go = Selection.activeGameObject;
			if (go != null)
			{
				var parent = go.transform.parent.gameObject;
				if (parent != null) {
					Selection.activeGameObject = parent;
					successfull = true;
				}
			}

			return successfull;
		}
	}
}
