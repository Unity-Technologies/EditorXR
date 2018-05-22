
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    /// <summary>
    /// Provides for smooth translation and/or rotation of an object
    /// </summary>
    sealed class SmoothMotion : MonoBehaviour, IUsesViewerScale
    {
        const float k_DefaultTighteningAmount = 20f;

        /// <summary>
        /// If true, smooth the rotation of this transform, according to the TightenRotation amount
        /// </summary>
        public bool smoothRotation { set { m_SmoothRotation = value; } }

        [Header("Rotation")]
        [SerializeField]
        bool m_SmoothRotation;

        [SerializeField]
        float m_TightenRotation = k_DefaultTighteningAmount;

        /// <summary>
        /// If true, smooth the position of this transform, according to the TightenPosition amount
        /// </summary>
        public bool smoothPosition { set { m_SmoothPosition = value; } }

        [Header("Position")]
        [SerializeField]
        bool m_SmoothPosition;

        [SerializeField]
        float m_TightenPosition = k_DefaultTighteningAmount;

        [Header("Optional")]
        [SerializeField]
        Transform m_Target;

        Quaternion m_LazyRotation;
        Vector3 m_LazyPosition;

        void Start()
        {
            if (m_Target == null && transform.parent != null)
            {
                m_Target = transform.parent;
                m_LazyRotation = transform.rotation;
            }
        }

        void OnEnable()
        {
            m_LazyPosition = transform.position;
            m_LazyRotation = transform.rotation;
        }

        void LateUpdate()
        {
            if (m_Target != transform.parent)
                m_Target = transform.parent; // cache new parent as this transform is assigned to different objects

            if (m_Target == null)
                return;

            var scaledTime = Time.deltaTime / this.GetViewerScale();
            const float kMaxSmoothingVelocity = 1f; // m/s
            var targetPosition = m_Target.position;
            if (Vector3.Distance(targetPosition, m_LazyPosition) > kMaxSmoothingVelocity * scaledTime)
            {
                m_LazyPosition = transform.position;
                m_LazyRotation = transform.rotation;
                return;
            }

            if (m_SmoothRotation)
            {
                var targetRotation = m_Target.rotation;
                m_LazyRotation = Quaternion.Lerp(m_LazyRotation, targetRotation, m_TightenRotation * scaledTime);
                transform.rotation = m_LazyRotation;
            }

            if (m_SmoothPosition)
            {
                m_LazyPosition = Vector3.Lerp(m_LazyPosition, targetPosition, m_TightenPosition * scaledTime);
                transform.position = m_LazyPosition;
            }
        }

        /// <summary>
        /// Set the transform that this object should follow
        /// </summary>
        /// <param name="target">The transform to follow</param>
        public void SetTarget(Transform target)
        {
            m_Target = target;
        }

        /// <summary>
        /// Setup rotation smoothing
        /// </summary>
        /// <param name="tightenAmount">A value of zero allows for full rotation smoothing, a value of 20 tightens greatly the rotation smoothing</param>
        public void SetRotationSmoothing(float tightenAmount = k_DefaultTighteningAmount)
        {
            m_SmoothRotation = true;
            m_TightenRotation = tightenAmount;
        }

        /// <summary>
        /// Setup position smoothing
        /// </summary>
        /// <param name="tightenAmount">A value of zero allows for full position smoothing, a value of 20 tightens greatly the position smoothing</param>
        public void SetPositionSmoothing(float tightenAmount = k_DefaultTighteningAmount)
        {
            m_SmoothPosition = true;
            m_TightenPosition = tightenAmount;
        }
    }
}

