using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// For specifying an alternate preview camera
    /// </summary>
    public interface IPreviewCamera
    {
        /// <summary>
        /// The custom preview camera
        /// </summary>
        Camera previewCamera { get; }

        /// <summary>
        /// The actual HMD camera (will be provided by system)
        /// </summary>
        Camera vrCamera { set; }

        /// <summary>
        /// A layer mask that controls what will always render in the HMD and not in the preview
        /// </summary>
        int hmdOnlyLayerMask { get; }

        /// <summary>
        /// Enable or disable the preview camera
        /// </summary>
        bool enabled { get; set; }
    }
}
