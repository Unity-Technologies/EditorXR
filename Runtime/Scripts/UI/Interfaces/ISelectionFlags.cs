namespace Unity.Labs.EditorXR.UI
{
    /// <summary>
    /// Allows fine-grained control of what constitutes a selection
    /// </summary>
    interface ISelectionFlags
    {
        /// <summary>
        /// Flags to control selection
        /// </summary>
        SelectionFlags selectionFlags { get; set; }
    }
}
