#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class GazeDivergenceModule : MonoBehaviour
    {
        Transform m_GazeSourceTransform;
        Quaternion m_PreviousGazeRotation;
        float m_GazeVelocity;

        void Awake()
        {
            m_GazeSourceTransform = CameraUtils.GetMainCamera().transform;
        }

        void Update()
        {
            var gazeRotationDifference = Quaternion.Angle(m_GazeSourceTransform.rotation, m_PreviousGazeRotation);
            m_GazeVelocity += gazeRotationDifference;
            m_GazeVelocity -= Time.unscaledDeltaTime;
            m_PreviousGazeRotation = m_GazeSourceTransform.rotation; // Cache the previous camera rotation
        }

        /// <summary>
        /// Test for a transform residing with a defined angular divergence threshold
        /// </summary>
        /// <param name="objectToTest">Vector to test for a threshold cross with relation to the gazeSource forward vector</param>
        /// <param name="divergenceThreshold">Threshold, in degrees, via doc product conversion of this angular value</param>
        /// <returns></returns>
        public bool IsAboveDivergenceThreshold(Transform objectToTest, float divergenceThreshold)
        {
            var gazeDirection = m_GazeSourceTransform.forward;
            var testVector = objectToTest.position - m_GazeSourceTransform.position; // Test object to gaze source vector
            testVector.Normalize(); // Normalize, in order to retain expected dot values

            var divergenceThresholdConvertedToDot = Mathf.Cos(Mathf.Deg2Rad* divergenceThreshold);
            var isAbove = Vector3.Dot(testVector, gazeDirection) > divergenceThresholdConvertedToDot;

            return isAbove;
        }
    }
}
#endif
