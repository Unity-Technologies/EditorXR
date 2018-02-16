#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.SpatialUI
{
    public class AdaptivePositionTest01 : MonoBehaviour, IAdaptPosition
    {
        const float k_AllowedGazeDivergence = 0.5f;

        public Transform transform { get; set; }
        public bool beingMoved { get; set; }
        public float allowedGazeDivergence { get { return k_AllowedGazeDivergence; } }

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
