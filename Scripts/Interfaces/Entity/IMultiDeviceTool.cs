
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Declares a tool as one that should be spawned on all devices at once
    /// </summary>
    public interface IMultiDeviceTool
    {
        /// <summary>
        /// Whether this tool is on the device that selected the tool
        /// </summary>
        bool primary { set; }
    }
}

