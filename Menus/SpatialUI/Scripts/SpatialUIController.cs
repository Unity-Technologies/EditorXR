#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Mandates that derived classes implement core required SpatialUI controller functionality
    /// </summary>
    public abstract class SpatialUIController : MonoBehaviour, IRayVisibilitySettings, INodeToRay
    {
        // Pre-box bools to avoid allocation when setting them via reflection
        static object[] boxedTrueBool = new object[] {true};
        static object[] boxedFalseBool = new object[] {false};

        static bool s_Initialized;
        static bool s_SceneViewGizmosInOriginalState = true;
        static bool s_SelectionOutlineWasEnabledOnStart;
        static bool s_SelectionWireframeWasEnabledOnStart;

        readonly List<Node> controllingNodes = new List<Node>();

        protected bool sceneViewGizmosVisible
        {
            get { return s_SceneViewGizmosInOriginalState; }

            set
            {
                if (s_SceneViewGizmosInOriginalState == value)
                    return;

                s_SceneViewGizmosInOriginalState = value;

                if (s_SceneViewGizmosInOriginalState) // Restore scene view gizmo visibility state
                    SetSceneViewGizmoStates(s_SelectionOutlineWasEnabledOnStart, s_SelectionWireframeWasEnabledOnStart);
                else // Hide gizmos if they were visible when startging the EXR session
                    SetSceneViewGizmoStates();
            }
        }

        void Awake()
        {
            if (!s_Initialized)
            {
                CacheSceneViewGizmoStates();
                s_Initialized = true;
            }
        }

        // TODO: add into separate branch handling dynamic focus changes of inputs
        protected void addControllingNode(Node node)
        {
            if (controllingNodes.Contains(node))
                return;

            controllingNodes.Add(node);

            // Set priority to 10, in order to suppress any standard ray visibility settings from overriding
            this.AddRayVisibilitySettings(this.RequestRayOriginFromNode(node), this, false, false, 10);

            Debug.LogWarning("HIDING ray for node : " + node.ToString());
        }

        // TODO: remove into separate branch handling dynamic focus changes of inputs
        protected void removeControllingNode(Node node)
        {
            if (!controllingNodes.Contains(node))
                return;

            controllingNodes.Remove(node);

            this.RemoveRayVisibilitySettings(this.RequestRayOriginFromNode(node), this);

            Debug.LogWarning("SHOWING ray for node : " + node.ToString());
        }

        static void CacheSceneViewGizmoStates()
        {
            s_SceneViewGizmosInOriginalState = true;

            // Disable the selection outline in the SceneView gizmos (popup)
            var annotation = Type.GetType("UnityEditor.Annotation, UnityEditor");
            var asm = Assembly.GetAssembly(typeof(Editor));
            var type = asm.GetType("UnityEditor.AnnotationUtility");
            if (type != null)
            {
                var currentSelectionOutlineProperty = type.GetProperty("showSelectionOutline", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                var currentSelectionOutlineValue = currentSelectionOutlineProperty.GetValue(type, null);
                if (currentSelectionOutlineValue != null)
                    s_SelectionOutlineWasEnabledOnStart = (bool) currentSelectionOutlineValue;

                var currentSelectionWireProperty = type.GetProperty("showSelectionWire", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                var currentSelectionWireValue = currentSelectionWireProperty.GetValue(type, null);
                if (currentSelectionWireValue != null)
                    s_SelectionWireframeWasEnabledOnStart = (bool) currentSelectionWireValue;
            }
        }

        static void SetSceneViewGizmoStates(bool selectionOutlineEnabled = false, bool selectionWireEnabled = false)
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

        protected static void ConsumeControls(SpatialMenuInput spatialMenuActionMapInput, ConsumeControlDelegate consumeControl, bool consumeSelection = true)
        {
            consumeControl(spatialMenuActionMapInput.cancel);
            consumeControl(spatialMenuActionMapInput.show);

            if (!consumeSelection)
                return;

            consumeControl(spatialMenuActionMapInput.confirm);
            consumeControl(spatialMenuActionMapInput.select);
        }
    }
}
#endif
