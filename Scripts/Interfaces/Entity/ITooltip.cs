
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates classes which can specify tool tip information
    /// </summary>
    public interface ITooltip
    {
        /// <summary>
        /// The text to display on hover
        /// </summary>
        string tooltipText { get; }
    }
}

