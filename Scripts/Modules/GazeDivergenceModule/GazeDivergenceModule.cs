#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class GazeDivergenceModule : MonoBehaviour
    {
        const float k_StableGazeThreshold = 0.25f;

        Transform m_GazeSourceTransform;
        Quaternion m_PreviousGazeRotation;
        float m_GazeVelocity;

        public bool gazeStable { get { return m_GazeVelocity < k_StableGazeThreshold; } }

        void Awake()
        {
            m_GazeSourceTransform = CameraUtils.GetMainCamera().transform;
        }

        void Update()
        {
            var gazeRotationDifference = Quaternion.Angle(m_GazeSourceTransform.rotation, m_PreviousGazeRotation);
            gazeRotationDifference *= gazeRotationDifference; // Square the difference for intended response curve/shape
            m_GazeVelocity = m_GazeVelocity + gazeRotationDifference * Time.unscaledDeltaTime;
            m_GazeVelocity = Mathf.Clamp01(m_GazeVelocity -= Time.unscaledDeltaTime);
            m_PreviousGazeRotation = m_GazeSourceTransform.rotation; // Cache the previous camera rotation
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
    }
}
#endif
