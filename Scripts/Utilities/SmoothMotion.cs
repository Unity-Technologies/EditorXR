namespace UnityEngine.VR.Utilities
{
	public class SmoothMotion : MonoBehaviour
	{
		[Header("Rotation")]
		[SerializeField]
		bool m_SmoothRotation;

		[SerializeField]
		float m_TightenRotation = 20f;

		[Header("Position")]
		[SerializeField]
		bool m_SmoothPosition;

		[SerializeField]
		float m_TightenPosition = 20f;

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

		void LateUpdate ()
		{
			if (m_Target != transform.parent)
				m_Target = transform.parent; // cache new parent as this transform is assigned to different objects

			if (m_Target == null)
				return;

			if (m_SmoothRotation)
			{
				Quaternion targetRotation = m_Target.rotation;
				m_LazyRotation = Quaternion.Lerp(m_LazyRotation, targetRotation, m_TightenRotation * Time.unscaledDeltaTime);
				transform.rotation = m_LazyRotation;
			}

			if (m_SmoothPosition)
			{
				Vector3 targetPosition = m_Target.position;
				m_LazyPosition = Vector3.Lerp(m_LazyPosition, targetPosition, m_TightenPosition * Time.unscaledDeltaTime);
				transform.position = m_LazyPosition;
			}
		}
	}
}
