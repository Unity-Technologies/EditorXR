namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Undo", ActionMenuItemAttribute.DefaultActionSectionName, 2)]
	internal sealed class Undo : BaseAction
	{
		public override void ExecuteAction()
		{
#if UNITY_EDITOR
			UnityEditor.Undo.PerformUndo();
#endif
		}
	}
}