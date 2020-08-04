namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Possible states for a spatial hint
    /// </summary>
    public enum SpatialHintState
    {
        /// <summary>
        /// The hint is hidden
        /// </summary>
        Hidden,

        /// <summary>
        /// The hint is ready for drag reveal
        /// </summary>
        PreDragReveal,

        /// <summary>
        /// The hint is indicating scrolling
        /// </summary>
        Scrolling,

        /// <summary>
        /// The hint is indicating centered scrolling
        /// </summary>
        CenteredScrolling,
    }
}
