using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class IntersectionTester : MonoBehaviour
    {
        [SerializeField]
        Transform[] m_RayTransforms;

        [SerializeField]
        bool m_ShowRays;

        bool m_Active = true;
        Ray[] m_Rays;
        int[] m_Triangles;
        Vector3[] m_Vertices;
        Collider m_Collider;

        public bool active
        {
            get { return m_Active && gameObject.activeInHierarchy; }
            set { m_Active = value; }
        }

        public Ray[] rays
        {
            get
            {
                if (m_Rays == null || m_Rays.Length != m_RayTransforms.Length)
                {
                    m_Rays = new Ray[m_RayTransforms.Length];
                    for (var i = 0; i < m_RayTransforms.Length; i++)
                    {
                        var t = m_RayTransforms[i];
                        m_Rays[i] = new Ray(transform.InverseTransformPoint(t.position), transform.InverseTransformDirection(t.forward));
                    }
                }

                return m_Rays;
            }
        }

        public int[] triangles
        {
            get
            {
                if (m_Triangles == null)
                {
                    var mf = GetComponentInChildren<MeshFilter>();
                    var mesh = mf.sharedMesh;
                    m_Triangles = mesh.triangles;
                }

                return m_Triangles;
            }
        }

        public Vector3[] vertices
        {
            get
            {
                if (m_Vertices == null)
                {
                    var mf = GetComponentInChildren<MeshFilter>();
                    var mesh = mf.sharedMesh;
                    m_Vertices = mesh.vertices;
                }

                return m_Vertices;
            }
        }

#if !UNITY_EDITOR
#pragma warning disable 109
#endif

        public new Collider collider
        {
            get
            {
                if (!m_Collider)
                    m_Collider = GetComponentInChildren<Collider>();

                return m_Collider;
            }
        }

        void OnDrawGizmos()
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
