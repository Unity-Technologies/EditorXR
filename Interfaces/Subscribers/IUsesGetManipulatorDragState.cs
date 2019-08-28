using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to get the current manipulator drag state
    /// </summary>
    public interface IUsesGetManipulatorDragState : IFunctionalitySubscriber<IProvidesGetManipulatorDragState> { }

    public static class UsesGetManipulatorDragStateMethods
    {
        /// <summary>
        /// Returns whether the manipulator is in the dragging state
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <returns>Whether the manipulator is currently being dragged</returns>
        public static bool GetManipulatorDragState(this IUsesGetManipulatorDragState user)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.GetManipulatorDragState();
#endif
        }
    }
}
