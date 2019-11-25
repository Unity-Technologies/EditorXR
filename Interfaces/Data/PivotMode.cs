#if !UNITY_EDITOR
namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    ///   <para>Where is the tool handle placed.</para>
    /// </summary>
    public enum PivotMode
    {
        /// <summary>
        ///   <para>The tool handle is at the graphical center of the selection.</para>
        /// </summary>
        Center,
        /// <summary>
        ///   <para>The tool handle is on the pivot point of the active object.</para>
        /// </summary>
        Pivot,
    }
}
#endif
