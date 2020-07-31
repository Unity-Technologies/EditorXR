using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provides the ability to control spatial-hinting visuals.
    ///
    /// Spatial-Hinting visuals are displayed when performing a spatial-input action, such as spatial-scrolling
    /// These visual elements assist the user in seeing which spatial direction(s) will
    /// reveal/allow additional spatial interaction(s).
    /// </summary>
    public interface IProvidesControlSpatialHinting : IFunctionalityProvider
    {
        /// <summary>
        /// Set the spatial hint state
        /// </summary>
        /// <param name="state">SpatialHintState to set</param>
        void SetSpatialHintState(SpatialHintState state);

        /// <summary>
        /// Set the position of the spatial hint visuals
        /// </summary>
        /// <param name="position">The position at which the spatial hint visuals should be displayed</param>
        void SetSpatialHintPosition(Vector3 position);

        /// <summary>
        /// Set the rotation of the spatial hint visuals container game object
        /// </summary>
        /// <param name="rotation">The rotation to set on the spatial visuals</param>
        void SetSpatialHintContainerRotation(Quaternion rotation);

        /// <summary>
        /// Sets the target for the spatial hint visuals to look at while performing an animated show or hide
        /// </summary>
        /// <param name="target">The position to target</param>
        void SetSpatialHintShowHideRotationTarget(Vector3 target);

        /// <summary>
        /// Set the LookAt target
        /// </summary>
        /// <param name="position">The position the visuals should look at</param>
        void SetSpatialHintLookAtRotation(Vector3 position);

        /// <summary>
        /// Visually pulse the spatial-scroll arrows; the arrows shown when performing a spatial scroll
        /// </summary>
        void PulseSpatialHintScrollArrows();

        /// <summary>
        /// Set the magnitude at which the user will trigger spatial scrolling
        /// </summary>
        /// <param name="position">The position, whose magnitude from the origin will be used to detect an initiation of spatial scrolling</param>
        void SetSpatialHintDragThresholdTriggerPosition(Vector3 position);

        /// <summary>
        /// Set reference to the object, RayOrigin, controlling the Spatial Hint visuals
        /// Each control-object has it's spatial scrolling processed independently
        /// </summary>
        /// <param name="controlNode">Node on which spatial scrolling will be processed independently</param>
        void SetSpatialHintControlNode(Node controlNode);
    }
}
