namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Undo", ActionMenuItemAttribute.kDefaultActionSectionName, 2)]
	public class Undo : BaseAction
	{
		public override void ExecuteAction()
		{
			UnityEditor.Undo.PerformUndo();
		}
	}
}