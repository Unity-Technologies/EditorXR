namespace UnityEngine.VR.Modules
{
	[ExecuteInEditMode]
	public class IntersectionTester : MonoBehaviour
	{
		[SerializeField]
		private Transform[] m_RayTransforms;

		[SerializeField]
		private bool m_ShowRays = false;

		public bool active
		{
			get { return m_Active && gameObject.activeInHierarchy; }
			set { m_Active = value; }
		}
		private bool m_Active = true;

		public Ray[] rays
		{
			get
			{
				if (m_Rays == null || m_Rays.Length != m_RayTransforms.Length)
				{
					m_Rays = new Ray[m_RayTransforms.Length];
					for (int i = 0; i < m_RayTransforms.Length; i++)
					{
						Transform t = m_RayTransforms[i];
						m_Rays[i] = new Ray(transform.InverseTransformPoint(t.position), transform.InverseTransformDirection(t.forward));
					}
				}

				return m_Rays;
			}
		}
		private Ray[] m_Rays;

		public new Renderer renderer
		{
			get
			{
				if (!m_Renderer)
					m_Renderer = GetComponent<Renderer>();
				return m_Renderer;
			}
		}
		private Renderer m_Renderer;

		public Renderer grabbedObject { get; set; }

		private void OnDrawGizmosSelected()
		{
			if (m_ShowRays)
			{
				foreach (var ray in rays)
				{
					Debug.DrawRay(transform.TransformPoint(ray.origin), transform.TransformDirection(ray.direction), Color.red);
				}
			}
		}
	}
}