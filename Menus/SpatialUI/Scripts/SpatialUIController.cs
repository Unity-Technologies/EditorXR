#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Mandates that derived classes implement core required SpatialUI controller functionality
    /// </summary>
    public abstract class SpatialUIController : MonoBehaviour, INodeToRay
    {
        // Pre-box fields to avoid allocation when setting them via reflection
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

        protected bool sceneViewGizmosVisible
        {
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

        static void CacheSceneViewGizmoStates()
        {
            // Cache the selection-outline & selection-wireframe in the SceneView gizmo states
            // These are set via checkbox in an Editor Scene/Game view/window/panel
            s_SceneViewGizmosInOriginalState = true;
            var asm = Assembly.GetAssembly(typeof(Editor));
            var annotationUtilityType = "UnityEditor.AnnotationUtility";
            var annotationType = "UnityEditor.Annotation, UnityEditor";
            s_SceneViewGizmoAnnotationUtilityType = asm.GetType(annotationUtilityType);
            s_AnnotationUtilityType = Type.GetType(annotationType);
            if (s_SceneViewGizmoAnnotationUtilityType != null)
            {
                var currentSelectionOutlineProperty = s_SceneViewGizmoAnnotationUtilityType.GetProperty(s_SelectionOutlineProperty, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                var currentSelectionOutlineValue = currentSelectionOutlineProperty.GetValue(s_SceneViewGizmoAnnotationUtilityType, null);
                if (currentSelectionOutlineValue != null)
                    s_SelectionOutlineWasEnabledOnStart = (bool)currentSelectionOutlineValue;

                var currentSelectionWireProperty = s_SceneViewGizmoAnnotationUtilityType.GetProperty(s_SelectionWireframeProperty, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                var currentSelectionWireValue = currentSelectionWireProperty.GetValue(s_SceneViewGizmoAnnotationUtilityType, null);
                if (currentSelectionWireValue != null)
                    s_SelectionWireframeWasEnabledOnStart = (bool)currentSelectionWireValue;
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
            consumeControl(spatialMenuActionMapInput.leftStickX);
            consumeControl(spatialMenuActionMapInput.leftStickY);
            consumeControl(spatialMenuActionMapInput.grip);

            if (!consumeSelection)
                return;

            consumeControl(spatialMenuActionMapInput.confirm);
            consumeControl(spatialMenuActionMapInput.select);
        }
    }
}
#endif
