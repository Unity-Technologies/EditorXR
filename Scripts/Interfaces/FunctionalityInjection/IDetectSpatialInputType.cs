#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to detect the current spatial-input for a given node
    /// Spatial UI & UX can/should respond, based on a given node's spatial input type:
    /// (translation, single axis rotation, free rotation, etc)
    /// </summary>
    public interface IDetectSpatialInputType : ICustomActionMap
    {
        // Func that takes a node, and returns the current TEMPORAL spatial input type detected for that node
        //Func<Node, SpatialInputType> getSpatialInputTypeForNode { get; set; }

        bool pollingSpatialInputType { get; set; }
    }

    public static class IDetectSpatialInputTypeMethods
    {
        internal delegate SpatialInputType GetSpatialInputTypeForNodeDelegate(IDetectSpatialInputType caller, Node node);

        internal static GetSpatialInputTypeForNodeDelegate getSpatialInputTypeForNode { private get; set; }

        /// <summary>
        /// Detect the active/current spatial input type of a given node
        /// </summary>
        /// "obj" : The caller polling for a node's spatial input type
        /// "node" : The node whose spatial input type will be returned
        public static SpatialInputType GetSpatialInputTypeForNode(this IDetectSpatialInputType obj, Node node)
        {
            return getSpatialInputTypeForNode(obj, node);
        }
    }
}
#endif
