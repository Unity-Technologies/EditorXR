namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Redo", "Assets/EditorVR/Actions/Icons/RedoIcon.png", ActionMenuItemAttribute.kDefaultActionSectionName, 1)]
	public class Redo : MonoBehaviour, IAction
	{
		public bool ExecuteAction()
		{
			UnityEditor.Undo.PerformRedo();
			return true;
		}
	}
}