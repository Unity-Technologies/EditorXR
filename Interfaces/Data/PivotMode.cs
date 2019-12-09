#if !UNITY_EDITOR
namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Where is the tool handle placed.
    /// </summary>
    public enum PivotMode
    {
        /// <summary>
        /// The tool handle is at the graphical center of the selection.
        /// </summary>
        Center,
        /// <summary>
        /// The tool handle is on the pivot point of the active object.
        /// </summary>
        Pivot,
    }
}
#endif
