#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    public class AdaptivePositionTest01 : MonoBehaviour, IAdaptPosition
    {
        const float k_AllowedGazeDivergence = 0.5f;

        public Transform adaptiveTransform { get; set; }
        public bool beingMoved { get; set; }
        public float allowedGazeDivergence { get { return k_AllowedGazeDivergence; } }
        public float m_DistanceOffset { get; private set; }
        public AdaptivePositionModule.AdaptivePositionData adaptivePositionData { get; set; }

        // Use this for initialization
        void Start()
        {
            if (allowedGazeDivergence > 0.5f)
                Debug.LogWarning("allowedGazeDivergence : " + k_AllowedGazeDivergence.ToString());
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
#endif
