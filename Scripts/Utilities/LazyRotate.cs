namespace UnityEngine.VR.Utilities
{
	public class LazyRotate : MonoBehaviour
	{
		[SerializeField]
		private Transform m_Target;	
		[SerializeField]
		private float m_Tighten = 20f;
		private Quaternion m_LazyRotation;

		private void Start()
		{
			if (m_Target == null && transform.parent != null)
			{
				m_Target = transform.parent;
				m_LazyRotation = transform.rotation;
			}
		}

		private void LateUpdate ()
		{
			if (m_Target != transform.parent)
				m_Target = transform.parent; // cache new parent as this transform is assigned to different objects

			if (m_Target == null)
				return;

			Quaternion targetRotation = m_Target.rotation;
			m_LazyRotation = Quaternion.Lerp(m_LazyRotation, targetRotation, m_Tighten * Time.unscaledDeltaTime);
			transform.rotation = m_LazyRotation;
		}
	}
}
