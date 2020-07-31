namespace Unity.EditorXR
{
    [ActionMenuItem("Redo", ActionMenuItemAttribute.DefaultActionSectionName, 1)]
    [SpatialMenuItem("Redo", "Actions", "Redo the previously undone action")]
    sealed class Redo : BaseAction
    {
        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.PerformRedo();
#endif
        }
    }
}
