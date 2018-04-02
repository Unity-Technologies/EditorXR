#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to detect the current spatial-input for a given node
    /// Spatial UI & UX can/should respond, based on a given node's spatial input type:
    /// (translation, single axis rotation, free rotation, etc)
    /// </summary>
    public interface IDetectSpatialInputType
    {
        //Func that takes a node, and returns the current TEMPORAL spatial input type detected for that node
        Func<Node, SpatialInputType> getSpatialInputTypeForNode { get; set; }
    }

    public static class IDetectSpatialInputTypeMethods
    {
    }
}
#endif
