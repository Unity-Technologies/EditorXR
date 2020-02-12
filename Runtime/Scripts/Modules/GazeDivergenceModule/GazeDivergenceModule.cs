using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Modules
{
    /// <summary>
    /// Allows an implementer to test for a given transforms'
    /// position residing within an angular threshold of the HMD
    /// </summary>
    sealed class GazeDivergenceModule : IModuleBehaviorCallbacks, IDelayedInitializationModule, IProvidesDetectGazeDivergence
    {
        const float k_StableGazeThreshold = 0.25f;

        Transform m_GazeSourceTransform;
        Quaternion m_PreviousGazeRotation;
        float m_GazeVelocity;
        float m_DivergenceSpeedScalar = 1f;

        /// <summary>
        /// Is the gaze currently focused on a single location, and not scanning the surrounding FOV above a certain velocity
        /// </summary>
        bool gazeStable { get { return m_GazeVelocity < k_StableGazeThreshold; } }

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }

        public void LoadModule() { }

        public void Initialize()
        {
            m_GazeSourceTransform = CameraUtils.GetMainCamera().transform;
            m_PreviousGazeRotation = m_GazeSourceTransform.rotation; // Prevent a quick initial snap of interpolated rotation values
        }

        public void Shutdown()
        {
        }

        public void UnloadModule() { }

        public void OnBehaviorUpdate()
        {
            if (m_GazeSourceTransform == null)
                return;

            var currentGazeSourceRotation = m_GazeSourceTransform.rotation;
            var gazeRotationDifference = Quaternion.Angle(currentGazeSourceRotation, m_PreviousGazeRotation);
            gazeRotationDifference *= gazeRotationDifference; // Square the difference for intended response curve/shape
            m_GazeVelocity = m_GazeVelocity + gazeRotationDifference * Time.unscaledDeltaTime;
            m_GazeVelocity = Mathf.Clamp01(m_GazeVelocity - Time.unscaledDeltaTime * m_DivergenceSpeedScalar);
            m_PreviousGazeRotation = currentGazeSourceRotation; // Cache the previous camera rotation
        }

        /// <summary>
        /// Set the value that scales the rate at which the gaze velocity will return to the stable threshold (below the gaze divergence threshold)
        /// A value of 1 will allow the gaze velocity to return to the stable threshold value at its' normal rate
        /// A value less than 1 will increase the rate at which the gaze velocity returns to the stable gaze threshold value (slower)
        /// A value greater than 1 will increase the rate at which the gaze velocity returns to the stable gaze threshold value (faster)
        /// </summary>
        /// <param name="rateAtWhichGazeVelocityReturnsToStableThreshold">The rate at which gaze velocity returns to stable threshold</param>
        public void SetDivergenceRecoverySpeed (float rateAtWhichGazeVelocityReturnsToStableThreshold)
        {
            const float minSpeed = 0.01f;
            rateAtWhichGazeVelocityReturnsToStableThreshold = Mathf.Abs(rateAtWhichGazeVelocityReturnsToStableThreshold); // don't allow negative values
            if (Mathf.Approximately(rateAtWhichGazeVelocityReturnsToStableThreshold, 0f))
                rateAtWhichGazeVelocityReturnsToStableThreshold = minSpeed; // prevent the gaze velocity from never being able to return to the stable threshold

            m_DivergenceSpeedScalar = rateAtWhichGazeVelocityReturnsToStableThreshold;
        }

        /// <summary>
        /// Test for a transform residing with a defined angular divergence threshold
        /// </summary>
        /// <param name="objectToTest">Vector to test for a threshold cross with relation to the gazeSource forward vector</param>
        /// <param name="divergenceThreshold">Threshold, in degrees, via doc product conversion of this angular value</param>
        /// <param name="disregardTemporalStability">If true, mandate that divergence detection occur, regardless of the gaze being stable</param>
        /// <returns>True if the object is beyond the divergence threshold, False if it is within the defined range</returns>
        public bool IsAboveDivergenceThreshold(Transform objectToTest, float divergenceThreshold, bool disregardTemporalStability = true)
        {
            if (m_GazeSourceTransform == null)
                return false;

            var isAbove = false;
            if (disregardTemporalStability || gazeStable) // validate that the gaze is stable if disregarding temporal stability
            {
                var gazeDirection = m_GazeSourceTransform.forward;
                var testVector = objectToTest.position - m_GazeSourceTransform.position; // Test object to gaze source vector
                testVector.Normalize(); // Normalize, in order to retain expected dot values

                var divergenceThresholdConvertedToDot = Mathf.Sin(Mathf.Deg2Rad * divergenceThreshold);
                var angularComparison = Mathf.Abs(Vector3.Dot(testVector, gazeDirection));
                isAbove = angularComparison < divergenceThresholdConvertedToDot;
            }

            return isAbove;
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var detectGazeDivergenceSubscriber = obj as IFunctionalitySubscriber<IProvidesDetectGazeDivergence>;
            if (detectGazeDivergenceSubscriber != null)
                detectGazeDivergenceSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
