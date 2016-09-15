namespace UnityEngine.VR.Utilities
{
	public class LazyRotate : MonoBehaviour
	{
		[SerializeField]
		private Transform m_TargetTransform;
		
		[SerializeField]
		private float m_DampingReduction = 5f;

		private Quaternion lazyRotation;

		private void Start()
		{
			if (m_TargetTransform == null)
				m_TargetTransform = transform.parent;

			if (m_TargetTransform != null)
				lazyRotation = transform.rotation;
		}

		private void LateUpdate ()
		{
			if (m_TargetTransform == null)
				return;

			Quaternion targetRotation = m_TargetTransform.rotation;
			lazyRotation = Quaternion.Lerp(lazyRotation, targetRotation, m_DampingReduction * Time.unscaledDeltaTime);
			transform.rotation = lazyRotation;
		}
	}
}