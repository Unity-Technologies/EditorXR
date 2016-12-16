namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Undo", ActionMenuItemAttribute.kDefaultActionSectionName, 2)]
	public class Undo : BaseAction
	{
		public override void ExecuteAction()
		{
#if UNITY_EDITOR
			UnityEditor.Undo.PerformUndo();
#endif
		}
	}
}