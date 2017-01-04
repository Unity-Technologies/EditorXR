namespace UnityEngine.Experimental.EditorVR.Helpers
{
	/// <summary>
	/// Provides for smooth translation and/or rotation of an object
	/// </summary>
	public class SmoothMotion : MonoBehaviour
	{
		const float kDefaultTighteningAmount = 20f;

		/// <summary>
		/// If true, smooth the rotation of this transform, according to the TightenRotation amount
		/// </summary>
		public bool smoothRotation { private get { return m_SmoothRotation; } set { m_SmoothRotation = value; } }
		[Header("Rotation")]
		[SerializeField]
		bool m_SmoothRotation;

		[SerializeField]
		float m_TightenRotation = kDefaultTighteningAmount;

		/// <summary>
		/// If true, smooth the position of this transform, according to the TightenPosition amount
		/// </summary>
		public bool smoothPosition { private get { return m_SmoothPosition; } set { m_SmoothPosition = value; } }
		[Header("Position")]
		[SerializeField]
		bool m_SmoothPosition;

		[SerializeField]
		float m_TightenPosition = kDefaultTighteningAmount;

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

			const float kMaxSmoothingVelocity = 1f; // m/s
			var targetPosition = m_Target.position;
			if (Vector3.Distance(targetPosition, m_LazyPosition) > kMaxSmoothingVelocity * Time.unscaledDeltaTime)
			{
				m_LazyPosition = transform.position;
				m_LazyRotation = transform.rotation;
				return;
			}

			if (m_SmoothRotation)
			{
				var targetRotation = m_Target.rotation;
				m_LazyRotation = Quaternion.Lerp(m_LazyRotation, targetRotation, m_TightenRotation * Time.unscaledDeltaTime);
				transform.rotation = m_LazyRotation;
			}

			if (m_SmoothPosition)
			{
				m_LazyPosition = Vector3.Lerp(m_LazyPosition, targetPosition, m_TightenPosition * Time.unscaledDeltaTime);
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
		public void SetRotationSmoothing(float tightenAmount = kDefaultTighteningAmount)
		{
			m_SmoothRotation = true;
			m_TightenRotation = tightenAmount;
		}

		/// <summary>
		/// Setup position smoothing
		/// </summary>
		/// <param name="tightenAmount">A value of zero allows for full position smoothing, a value of 20 tightens greatly the position smoothing</param>
		public void SetPositionSmoothing(float tightenAmount = kDefaultTighteningAmount)
		{
			m_SmoothPosition = true;
			m_TightenPosition = tightenAmount;
		}
	}
}
