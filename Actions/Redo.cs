namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Redo", ActionMenuItemAttribute.kDefaultActionSectionName, 1)]
	public class Redo : BaseAction
	{
		public override void ExecuteAction()
		{
			UnityEditor.Undo.PerformRedo();
		}
	}
}