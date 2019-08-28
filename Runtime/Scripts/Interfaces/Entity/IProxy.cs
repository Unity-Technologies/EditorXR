using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Declares a class as being a proxy for an input device
    /// </summary>
    public interface IProxy
    {
        /// <summary>
        /// Whether the proxy is present and active
        /// </summary>
        bool active { get; }

        /// <summary>
        /// Event called when the active property changes
        /// </summary>
        event Action activeChanged;

        /// <summary>
        /// Provided to a proxy for device input (e.g. position / rotation)
        /// </summary>
        TrackedObject trackedObjectInput { set; }

        /// <summary>
        /// The ray origin for each proxy node
        /// </summary>
        Dictionary<Node, Transform> rayOrigins { get; }

        /// <summary>
        /// Whether the proxy is not visible
        /// </summary>
        bool hidden { set; }

        /// <summary>
        /// Origins for where menus show (e.g. main menu)
        /// Key = ray origin
        /// Value = preview transform
        /// </summary>
        Dictionary<Transform, Transform> menuOrigins { get; set; }

        /// <summary>
        /// Origins for alternate menus
        /// Key = ray origin
        /// Value = alternate menu transform
        /// </summary>
        Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }

        /// <summary>
        /// Origins for asset previews
        /// Key = ray origin
        /// Value = preview transform
        /// </summary>
        Dictionary<Transform, Transform> previewOrigins { get; set; }

        /// <summary>
        /// Origins for grabbed list fields
        /// Key = ray origin
        /// Value = field grab transform
        /// </summary>
        Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }
    }
}
