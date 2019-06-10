using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Decorates classes that provide positioning information for tooltips
    /// </summary>
    public interface ITooltipPlacement
    {
        /// <summary>
        /// The transform relative to which the tooltip will display
        /// </summary>
        Transform tooltipTarget { get; }

        /// <summary>
        /// The transform to which the dotted line connects
        /// </summary>
        Transform tooltipSource { get; }

        /// <summary>
        /// Whether to align the left side, right side, or center of the tooltip to the target
        /// </summary>
        TextAlignment tooltipAlignment { get; }
    }
}
