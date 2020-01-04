using Unity.Labs.EditorXR.Modules;
using Unity.Labs.EditorXR.UI;

namespace Unity.Labs.EditorXR.Utilities
{
    /// <summary>
    /// UI related utilities
    /// </summary>
    static class UIUtils
    {
        /// <summary>
        /// Maximum interval between clicks that count as a double-click
        /// </summary>
        public const float DoubleClickIntervalMax = 0.3f;

        const float k_DoubleClickIntervalMin = 0.15f;

        /// <summary>
        /// Returns whether the given time interval qualifies as a double-click
        /// </summary>
        /// <param name="timeSinceLastClick">Time interval between clicks</param>
        /// <returns></returns>
        public static bool IsDoubleClick(float timeSinceLastClick)
        {
            return timeSinceLastClick <= DoubleClickIntervalMax && timeSinceLastClick >= k_DoubleClickIntervalMin;
        }

        public static bool IsDirectEvent(RayEventData eventData)
        {
            return eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.distance <= eventData.pointerLength || eventData.dragging;
        }

        public static bool IsValidEvent(RayEventData eventData, SelectionFlags selectionFlags)
        {
            if ((selectionFlags & SelectionFlags.Direct) != 0 && IsDirectEvent(eventData))
                return true;

            if ((selectionFlags & SelectionFlags.Ray) != 0)
                return true;

            return false;
        }
    }
}
