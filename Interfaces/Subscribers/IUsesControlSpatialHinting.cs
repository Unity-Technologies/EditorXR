using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class ability to control spatial-hinting visuals.
    ///
    /// Spatial-Hinting visuals are displayed when performing a spatial-input action, such as spatial-scrolling
    /// These visual elements assist the user in seeing which spatial direction(s) will
    /// reveal/allow additional spatial interaction(s).
    /// </summary>
    public interface IUsesControlSpatialHinting : IFunctionalitySubscriber<IProvidesControlSpatialHinting>
    {
    }

    public static class UsesControlSpatialHintingMethods
    {
        /// <summary>
        /// Set the spatial hint state
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="state">SpatialHintState to set</param>
        public static void SetSpatialHintState(this IUsesControlSpatialHinting user, SpatialHintState state)
        {
#if !FI_AUTOFILL
            user.provider.SetSpatialHintState(state);
#endif
        }

        /// <summary>
        /// Set the position of the spatial hint visuals
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="position">The position at which the spatial hint visuals should be displayed</param>
        public static void SetSpatialHintPosition(this IUsesControlSpatialHinting user, Vector3 position)
        {
#if !FI_AUTOFILL
            user.provider.SetSpatialHintPosition(position);
#endif
        }

        /// <summary>
        /// Set the rotation of the spatial hint visuals container game object
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rotation">The rotation to set on the spatial visuals</param>
        public static void SetSpatialHintContainerRotation(this IUsesControlSpatialHinting user, Quaternion rotation)
        {
#if !FI_AUTOFILL
            user.provider.SetSpatialHintContainerRotation(rotation);
#endif
        }

        /// <summary>
        /// Sets the target for the spatial hint visuals to look at while performing an animated show or hide
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="target">The position to target</param>
        public static void SetSpatialHintShowHideRotationTarget(this IUsesControlSpatialHinting user, Vector3 target)
        {
#if !FI_AUTOFILL
            user.provider.SetSpatialHintShowHideRotationTarget(target);
#endif
        }

        /// <summary>
        /// Set the LookAt target
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="position">The position the visuals should look at</param>
        public static void SetSpatialHintLookAtRotation(this IUsesControlSpatialHinting user, Vector3 position)
        {
#if !FI_AUTOFILL
            user.provider.SetSpatialHintLookAtRotation(position);
#endif
        }

        /// <summary>
        /// Visually pulse the spatial-scroll arrows; the arrows shown when performing a spatial scroll
        /// </summary>
        /// <param name="user">The functionality user</param>
        public static void PulseSpatialHintScrollArrows(this IUsesControlSpatialHinting user)
        {
#if !FI_AUTOFILL
            user.provider.PulseSpatialHintScrollArrows();
#endif
        }

        /// <summary>
        /// Set the magnitude at which the user will trigger spatial scrolling
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="position">The position, whose magnitude from the origin will be used to detect an initiation of spatial scrolling</param>
        public static void SetSpatialHintDragThresholdTriggerPosition(this IUsesControlSpatialHinting user, Vector3 position)
        {
#if !FI_AUTOFILL
            user.provider.SetSpatialHintDragThresholdTriggerPosition(position);
#endif
        }

        /// <summary>
        /// Set reference to the object, RayOrigin, controlling the Spatial Hint visuals
        /// Each control-object has it's spatial scrolling processed independently
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="controlNode">Node on which spatial scrolling will be processed independently</param>
        public static void SetSpatialHintControlNode(this IUsesControlSpatialHinting user, Node controlNode)
        {
#if !FI_AUTOFILL
            user.provider.SetSpatialHintControlNode(controlNode);
#endif
        }
    }
}
