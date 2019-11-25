namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Implementors can be dropped on IDropReceivers
    /// </summary>
    public interface IDroppable
    {
        /// <summary>
        /// Get the underlying object that will be dropped
        /// </summary>
        /// <returns>The object that will be dropped</returns>
        object GetDropObject();
    }
}
