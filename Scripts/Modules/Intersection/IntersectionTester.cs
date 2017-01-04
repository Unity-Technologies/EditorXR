namespace UnityEngine.Experimental.EditorVR.Modules
{
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

		public int[] triangles
		{
			get
			{
				if (m_Triangles == null)
				{
					var mf = GetComponent<MeshFilter>();
					var mesh = mf.sharedMesh;
					m_Triangles = mesh.triangles;
				}

				return m_Triangles;
			}
		}
		private int[] m_Triangles;

		public Vector3[] vertices
		{
			get
			{
				if (m_Vertices == null)
				{
					var mf = GetComponent<MeshFilter>();
					var mesh = mf.sharedMesh;
					m_Vertices = mesh.vertices;
				}

				return m_Vertices;
			}
		}
		private Vector3[] m_Vertices;

#if !UNITY_EDITOR
#pragma warning disable 109
#endif
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

		private void OnDrawGizmos()
		{
			if (m_ShowRays)
			{
				foreach (var t in m_RayTransforms)
				{
					if (t)
						Debug.DrawRay(t.position, t.forward, Color.red);
				}
			}
		}
	}
}