using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorXR.Core;
using Unity.EditorXR.Extensions;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;

namespace Unity.EditorXR.Modules
{
    sealed class AdaptivePositionModule : ScriptableSettings<AdaptivePositionModule>, IDelayedInitializationModule,
        IModuleBehaviorCallbacks, IUsesDetectGazeDivergence, IUsesViewerScale, IUsesControlHaptics, IInterfaceConnector
    {
#pragma warning disable 649
        [SerializeField]
        HapticPulse m_MovingPulse; // The pulse performed while moving an element to a new target position in the user's FOV
#pragma warning restore 649

        bool m_TestInFocus;

        [NonSerialized]
        Transform m_GazeTransform;

        [NonSerialized]
        Transform m_WorldspaceAnchorTransform; // The player transform under which anchored objects will be parented

        // Collection of objects whose position is controlled by this module
        readonly List<IAdaptPosition> m_AdaptivePositionElements = new List<IAdaptPosition>();

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
        IProvidesDetectGazeDivergence IFunctionalitySubscriber<IProvidesDetectGazeDivergence>.provider { get; set; }
        IProvidesControlHaptics IFunctionalitySubscriber<IProvidesControlHaptics>.provider { get; set; }
#endif

        public void LoadModule()
        {
            var mainCamera = CameraUtils.GetMainCamera();
            if (mainCamera == null)
                return;

            m_GazeTransform = mainCamera.transform;
            if (m_GazeTransform)
                m_WorldspaceAnchorTransform = m_GazeTransform.parent;
        }

        public void UnloadModule() { }

        public void OnBehaviorUpdate()
        {
            if (m_GazeTransform == null)
                return;

            if (m_AdaptivePositionElements.Count > 0)
            {
                foreach (var element in m_AdaptivePositionElements)
                {
                    var repositionCoroutine = element.adaptiveElementRepositionCoroutine;
                    if (element.resetAdaptivePosition)
                    {
                        EditorMonoBehaviour.instance.RestartCoroutine(ref repositionCoroutine, RepositionElement(element));
                        element.adaptiveElementRepositionCoroutine = repositionCoroutine;
                        return;
                    }

                    var adaptiveTransform = element.adaptiveTransform;
                    var allowedDegreeOfGazeDivergence = element.allowedDegreeOfGazeDivergence;
                    var isAboveDivergenceThreshold = this.IsAboveDivergenceThreshold(adaptiveTransform, allowedDegreeOfGazeDivergence);
                    element.inFocus = !isAboveDivergenceThreshold; // gaze divergence threshold test regardless of temporal stability

                    if (!element.allowAdaptivePositioning)
                        continue;

                    if (repositionCoroutine == null)
                    {
                        var moveElement = false;
                        var isAboveTemporalDivergenceThreshold = this.IsAboveDivergenceThreshold(adaptiveTransform, allowedDegreeOfGazeDivergence, false);
                        if (element.repositionIfOutOfFocus && isAboveTemporalDivergenceThreshold)
                        {
                            moveElement = true;
                        }
                        else
                        {
                            var distanceFromGazeTransform = Mathf.Abs(Vector3.Magnitude(m_GazeTransform.position - adaptiveTransform.position)) / this.GetViewerScale();
                            if (distanceFromGazeTransform > element.allowedMaxDistanceDivergence)
                            {
                                // only move if above the gaze divergence threshold with respect to temporal stability
                                if (element.onlyMoveWhenOutOfFocus && !isAboveTemporalDivergenceThreshold)
                                    continue;

                                moveElement = true;
                            }
                            else if (distanceFromGazeTransform < element.allowedMinDistanceDivergence)
                            {
                                // Always move away from the hmd regardless of angle, in order to prevent the user from moving through the UI
                                moveElement = true;
                            }
                        }

                        if (moveElement)
                        {
                            EditorMonoBehaviour.instance.RestartCoroutine(ref repositionCoroutine, RepositionElement(element));
                            element.adaptiveElementRepositionCoroutine = repositionCoroutine;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Implementers are assigned in the AdaptivePositionModuleConnector's ConnectInterface() function
        /// </summary>
        /// <param name="adaptiveElement">Implementer that will be added to the AdaptivePositionElements collection</param>
        public void ControlObject(IAdaptPosition adaptiveElement)
        {
            if (m_AdaptivePositionElements.Contains(adaptiveElement))
                return;

            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            adaptiveTransform.localPosition = new Vector3(0f, 0f, adaptiveElement.adaptivePositionRestDistance); // push the object away from the HMD
            adaptiveTransform.parent = m_WorldspaceAnchorTransform;
            adaptiveTransform.rotation = Quaternion.identity;

            m_AdaptivePositionElements.Add(adaptiveElement);
        }

        /// <summary>
        /// Implementers are removed from the AdaptivePositionModuleConnector's DisconnectInterface() function
        /// </summary>
        /// <param name="adaptiveElement">Implementer that will be removed from the AdaptivePositionElements collection</param>
        public void FreeObject(IAdaptPosition adaptiveElement)
        {
            if (m_AdaptivePositionElements.Contains(adaptiveElement))
                m_AdaptivePositionElements.Remove(adaptiveElement);
        }

        IEnumerator RepositionElement(IAdaptPosition adaptiveElement)
        {
            var adaptiveTransform = adaptiveElement.adaptiveTransform;
            var currentPosition = adaptiveTransform.position;
            var targetPosition = m_GazeTransform.position;
            targetPosition = targetPosition + this.GetViewerScale() * adaptiveElement.adaptivePositionRestDistance * m_GazeTransform.forward;
            if (!adaptiveElement.resetAdaptivePosition)
            {
                this.Pulse(Node.None, m_MovingPulse);
                const float kTransitionSpeedScalar = 4f;
                adaptiveElement.beingMoved = true;
                var transitionAmount = 0f;
                while (transitionAmount < 1f)
                {
                    var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                    smoothTransition *= smoothTransition;
                    adaptiveTransform.position = Vector3.Lerp(currentPosition, targetPosition, smoothTransition);
                    transitionAmount += Time.deltaTime * kTransitionSpeedScalar;
                    adaptiveTransform.LookAt(m_GazeTransform);
                    yield return null;
                }
            }

            adaptiveTransform.position = targetPosition;
            adaptiveTransform.LookAt(m_GazeTransform);
            adaptiveElement.adaptiveElementRepositionCoroutine = null;
            adaptiveElement.beingMoved = false;
            adaptiveElement.resetAdaptivePosition = false;
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var adaptsPosition = target as IAdaptPosition;
            if (adaptsPosition != null)
                ControlObject(adaptsPosition);
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var adaptsPosition = target as IAdaptPosition;
            if (adaptsPosition != null)
                FreeObject(adaptsPosition);
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void Initialize()
        {
            m_AdaptivePositionElements.Clear();
            m_GazeTransform = CameraUtils.GetMainCamera().transform;
            m_WorldspaceAnchorTransform = m_GazeTransform.parent;
        }

        public void Shutdown()
        {
            m_AdaptivePositionElements.Clear();
            m_GazeTransform = null;
            m_WorldspaceAnchorTransform = null;
        }
    }
}
