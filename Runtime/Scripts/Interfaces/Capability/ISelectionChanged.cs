namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Decorates types that need to respond to a change in selection
    /// </summary>
    public interface ISelectionChanged
    {
        /// <summary>
        /// Called when selection changes (via Selection.onSelectionChange subscriber)
        /// Use the Selection class to get selected objects
        /// </summary>
        void OnSelectionChanged();
    }
}
