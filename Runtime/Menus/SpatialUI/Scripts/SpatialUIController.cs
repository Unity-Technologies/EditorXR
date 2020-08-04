using System;
using System.Reflection;
using Unity.EditorXR.Core;
using Unity.EditorXR.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputNew;

namespace Unity.EditorXR.Menus
{
    /// <summary>
    /// Mandates that derived classes implement core required SpatialUI controller functionality
    /// </summary>
    abstract class SpatialUIController : MonoBehaviour, INodeToRay
    {
#if UNITY_EDITOR
        // Pre-box fields to avoid allocation when setting them via reflection
        static readonly object[] s_BoxedTrueBool = new object[] {true};
        static readonly object[] s_BoxedFalseBool = new object[] {false};
        static bool s_Initialized;
        static bool s_SceneViewGizmosInOriginalState = true;
        static bool s_SelectionOutlineWasEnabledOnStart;
        static bool s_SelectionWireframeWasEnabledOnStart;
        static Type s_SceneViewGizmoAnnotationUtilityType;
        static Type s_AnnotationUtilityType;
        static readonly string s_SelectionOutlineProperty = "showSelectionOutline";
        static readonly string s_SelectionWireframeProperty = "showSelectionWire";

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
                var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty;
                var currentSelectionOutlineProperty = s_SceneViewGizmoAnnotationUtilityType.GetProperty(s_SelectionOutlineProperty, flags);
                if (currentSelectionOutlineProperty != null)
                {
                    var currentSelectionOutlineValue = currentSelectionOutlineProperty.GetValue(s_SceneViewGizmoAnnotationUtilityType, null);
                    if (currentSelectionOutlineValue != null)
                        s_SelectionOutlineWasEnabledOnStart = (bool)currentSelectionOutlineValue;
                }

                var currentSelectionWireProperty = s_SceneViewGizmoAnnotationUtilityType.GetProperty(s_SelectionWireframeProperty, flags);
                if (currentSelectionWireProperty != null)
                {
                    var currentSelectionWireValue = currentSelectionWireProperty.GetValue(s_SceneViewGizmoAnnotationUtilityType, null);
                    if (currentSelectionWireValue != null)
                        s_SelectionWireframeWasEnabledOnStart = (bool)currentSelectionWireValue;
                }
            }
        }

        static void SetSceneViewGizmoStates(bool selectionOutlineEnabled = false, bool selectionWireEnabled = false)
        {
            // Enable/Disable values in the SceneView gizmos (editor scene/game view popup)
            // This functionality allows for hiding/showing of the outlines and wireframes that will draw above the SpatialUI element
            var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetProperty;
            var selectionOutlineEnabledBoxedBool = selectionOutlineEnabled ? s_BoxedTrueBool : s_BoxedFalseBool;
            s_SceneViewGizmoAnnotationUtilityType.InvokeMember(s_SelectionOutlineProperty,
                flags, Type.DefaultBinder, s_AnnotationUtilityType, selectionOutlineEnabledBoxedBool);

            var selectionWireEnabledBoxedBool = selectionWireEnabled ? s_BoxedTrueBool : s_BoxedFalseBool;
            s_SceneViewGizmoAnnotationUtilityType.InvokeMember(s_SelectionWireframeProperty,
                flags, Type.DefaultBinder, s_AnnotationUtilityType, selectionWireEnabledBoxedBool);
        }
#endif

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
