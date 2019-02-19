namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Undo", ActionMenuItemAttribute.DefaultActionSectionName, 2)]
    [SpatialMenuItem("Undo", "Actions", "Undo the previous action")]
    sealed class Undo : BaseAction
    {
        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.PerformUndo();
#endif
        }
    }
}
