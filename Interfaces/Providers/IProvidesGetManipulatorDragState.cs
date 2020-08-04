using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to get the current manipulator drag state
    /// </summary>
    public interface IProvidesGetManipulatorDragState : IFunctionalityProvider
    {
        /// <summary>
        /// Returns whether the manipulator is in the dragging state
        /// </summary>
        /// <returns>Whether the manipulator is currently being dragged</returns>
        bool GetManipulatorDragState();
    }
}
