namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Undo", "Assets/EditorVR/Actions/Icons/UndoIcon.png", ActionMenuItemAttribute.kDefaultActionSectionName, 2)]
	public class Undo : MonoBehaviour, IAction
	{
		public bool ExecuteAction()
		{
			UnityEditor.Undo.PerformUndo();
			return true;
		}
	}
}