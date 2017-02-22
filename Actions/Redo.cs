namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Redo", ActionMenuItemAttribute.kDefaultActionSectionName, 1)]
	public class Redo : BaseAction
	{
		public override void ExecuteAction()
		{
#if UNITY_EDITOR
			UnityEditor.Undo.PerformRedo();
#endif
		}
	}
}