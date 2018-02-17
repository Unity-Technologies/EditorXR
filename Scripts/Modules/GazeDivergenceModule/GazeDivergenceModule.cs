#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class GazeDivergenceModule : MonoBehaviour
    {
        Transform m_GazeSourceTransform;

        public class GazeDivergenceData
        {
            public GazeDivergenceData(Vector3 directionToTest, float divergenceThreshold)
            {
                this.directionToTest = directionToTest;
                this.divergenceThreshold = divergenceThreshold;
            }

            // Below is Data assigned by calling object requesting a divergence delta test

            /// <summary>
            /// The vector, whose divergence will be tested against the gaze source's forward vector
            /// </summary>
            public Vector3 directionToTest { get; set; }

            /// <summary>
            /// Degree, beyond which the divergence threshold will return TRUE if passed
            /// </summary>
            public float divergenceThreshold { get; set; }
        }

        void Awake()
        {
            m_GazeSourceTransform = CameraUtils.GetMainCamera().transform;
        }

        public bool IsAboveDivergenceThreshold(GazeDivergenceData data)
        {
            var isAbove = Vector3.Dot(data.directionToTest, m_GazeSourceTransform.forward) >= Mathf.Abs(data.divergenceThreshold);
            return isAbove;
        }
    }
}
#endif
