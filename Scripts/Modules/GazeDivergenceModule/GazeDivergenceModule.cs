#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class GazeDivergenceModule : MonoBehaviour
    {
        Transform m_GazeSourceTransform;

        void Awake()
        {
            m_GazeSourceTransform = CameraUtils.GetMainCamera().transform;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directionToTest">Vector to test for a threshold cross with relation to the gazeSource forward vector</param>
        /// <param name="divergenceThreshold">Threshold, in degrees, via doc product conversion of this angular value</param>
        /// <returns></returns>
        public bool IsAboveDivergenceThreshold(Vector3 directionToTest, float divergenceThreshold)
        {
            // if I type in 90, I want any dot value greater than zero to be false, and value less than zero to be true
            var divergenceThresholdConvertedToDot = Mathf.Cos(Mathf.Deg2Rad* divergenceThreshold);
            var isAbove = Vector3.Dot(directionToTest, m_GazeSourceTransform.forward) > divergenceThresholdConvertedToDot;
            Debug.LogError("divergenceThreshold : " + divergenceThreshold);
            Debug.LogError("divergenceThresholdConvertedToDot : " + divergenceThresholdConvertedToDot);
            Debug.LogError("Dot : " + Vector3.Dot(directionToTest, m_GazeSourceTransform.forward));
            Debug.LogError("isAbove : " + isAbove);
            return isAbove;
        }
    }
}
#endif
