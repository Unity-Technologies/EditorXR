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
        static Type s_SceneViewGizmoAnnotationUtilityType;
        static Type s_AnnotationUtilityType;
        static string s_SelectionOutlineProperty = "showSelectionOutline";
        static string s_SelectionWireframeProperty = "showSelectionWire";

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
            // Cache the selection-outline & selection-wireframe in the SceneView gizmo states
            // These are set via checkbox in an Editor Scene/Game view/window/panel
            s_SceneViewGizmosInOriginalState = true;
            var asm = Assembly.GetAssembly(typeof(Editor));
            s_SceneViewGizmoAnnotationUtilityType = asm.GetType("UnityEditor.AnnotationUtility");
            s_AnnotationUtilityType = Type.GetType("UnityEditor.Annotation, UnityEditor");
            if (s_SceneViewGizmoAnnotationUtilityType != null)
            {
                var currentSelectionOutlineProperty = s_SceneViewGizmoAnnotationUtilityType.GetProperty(s_SelectionOutlineProperty, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                var currentSelectionOutlineValue = currentSelectionOutlineProperty.GetValue(s_SceneViewGizmoAnnotationUtilityType, null);
                if (currentSelectionOutlineValue != null)
                    s_SelectionOutlineWasEnabledOnStart = (bool) currentSelectionOutlineValue;

                var currentSelectionWireProperty = s_SceneViewGizmoAnnotationUtilityType.GetProperty(s_SelectionWireframeProperty, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                var currentSelectionWireValue = currentSelectionWireProperty.GetValue(s_SceneViewGizmoAnnotationUtilityType, null);
                if (currentSelectionWireValue != null)
                    s_SelectionWireframeWasEnabledOnStart = (bool) currentSelectionWireValue;
            }
        }

        static void SetSceneViewGizmoStates(bool selectionOutlineEnabled = false, bool selectionWireEnabled = false)
        {
            // Enable/Disable values in the SceneView gizmos (editor scene/game view popup)
            // This functionality allows for hiding/showing of the outlines and wireframes that will draw above the SpatialUI element
            var selectionOutlineEnabledBoxedBool = selectionOutlineEnabled ? boxedTrueBool : boxedFalseBool;
            s_SceneViewGizmoAnnotationUtilityType.InvokeMember(s_SelectionOutlineProperty,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetProperty,
                Type.DefaultBinder, s_AnnotationUtilityType, selectionOutlineEnabledBoxedBool);

            s_SceneViewGizmoAnnotationUtilityType.InvokeMember(s_SelectionWireframeProperty,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetProperty,
                Type.DefaultBinder, s_AnnotationUtilityType, selectionOutlineEnabledBoxedBool);
        }

        protected static void ConsumeControls(SpatialMenuInput spatialMenuActionMapInput, ConsumeControlDelegate consumeControl, bool consumeSelection = true)
        {
            consumeControl(spatialMenuActionMapInput.cancel);
            consumeControl(spatialMenuActionMapInput.showMenu);

            if (!consumeSelection)
                return;

            consumeControl(spatialMenuActionMapInput.confirm);
            consumeControl(spatialMenuActionMapInput.select);
        }
    }
}
#endif
