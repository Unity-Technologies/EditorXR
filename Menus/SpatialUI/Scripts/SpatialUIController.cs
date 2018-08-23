#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Mandates that derived classes implement core required SpatialUI controller functionality
    /// </summary>
    public abstract class SpatialUIController : MonoBehaviour, IRayVisibilitySettings, INodeToRay
    {
        static object[] boxedTrueBool = new object[] {true};
        static object[] boxedFalseBool = new object[] {false};

        List<Node> controllingNodes { get; set; }

        protected void addControllingNode(Node node)
        {
            if (controllingNodes.Contains(node))
                return;

            controllingNodes.Add(node);

            // Set priority to 10, in order to suppress any standard ray visibility settings from overriding
            this.AddRayVisibilitySettings(this.RequestRayOriginFromNode(node), this, false, false, 10);
        }

        protected void removeControllingNode(Node node)
        {
            if (!controllingNodes.Contains(node))
                return;

            controllingNodes.Remove(node);

            this.RemoveRayVisibilitySettings(this.RequestRayOriginFromNode(node), this);
        }

        protected void SetSceneViewGizmoStates(bool selectionOutlineEnabled = false, bool selectionWireEnabled = false)
        {
            var selectionOutlineEnabledBoxedBool = selectionOutlineEnabled ? boxedTrueBool : boxedFalseBool;

            // Disable the selection outline in the SceneView gizmos (popup)
            var annotation = Type.GetType("UnityEditor.Annotation, UnityEditor");
            var asm = Assembly.GetAssembly(typeof(Editor));
            var type = asm.GetType("UnityEditor.AnnotationUtility");
            if (type != null)
            {
                type.InvokeMember("showSelectionOutline",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetProperty,
                    Type.DefaultBinder, annotation, selectionOutlineEnabledBoxedBool);

                type.InvokeMember("showSelectionWire",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetProperty,
                    Type.DefaultBinder, annotation, selectionOutlineEnabledBoxedBool);
            }
        }
    }
}
#endif
