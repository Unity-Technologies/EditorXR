namespace UnityEngine.VR.Data
{
	public class IntersectionTester
	{
		public Renderer renderer { get; private set; }
		public Ray[] rays { get; private set; }
		public SpatialObject grabbed { get; set; }
		public Transform oldParent { get; set; }

		private bool m_Active = true;

		public bool active
		{
			get { return m_Active && renderer.gameObject.activeInHierarchy; }
			set { m_Active = value; }
		}

		public IntersectionTester(Renderer renderer, Ray[] rays)
		{
			this.renderer = renderer;
			this.rays = rays;
		}

		public IntVector3 GetCell(SpatialHash hash)
		{
			return hash.SnapToGrid(renderer.bounds.center + Vector3.one * hash.cellSize * 0.5f);
		}

		public static Mesh GenerateConeMesh(int segments, float radius, float height, out Ray[] rays)
		{
			Mesh cone = new Mesh();
			//For hard edges, three verts at each bottom corner, and one at the top per segment
			Vector3[] vertices = new Vector3[(segments + 1) * 4];
			Vector3[] normals = new Vector3[(segments + 1) * 4];
			//One triangle per second on the cone, segments - 2 triangles on the base
			int[] triangles = new int[(segments + segments - 1) * 3];
			rays = new Ray[segments + 1];
			Vector3 top = Vector3.forward * height;
			rays[0] = new Ray(top, Vector3.forward);
			int stride = segments + 1;
			for (int j = 0; j <= segments; j++)
			{
				Vector3 radial = Quaternion.AngleAxis((float) j / segments * 360, Vector3.forward) * Vector3.up;
				Vector3 nextRadial = Quaternion.AngleAxis((float) (j + 1) / segments * 360, Vector3.forward) * Vector3.up;
				if (j + 1 >= segments)
					nextRadial = Vector3.up;
				Vector3 normal = new Vector3(0, radius, height).normalized;
				normal = Quaternion.AngleAxis((j + 0.5f) / segments * 360, Vector3.forward) * normal;
				vertices[j] = radial * radius;
				vertices[j + stride] = vertices[j];
				vertices[j + stride * 2] = nextRadial * radius;
				vertices[j + stride * 3] = top;
				normals[j] = Vector3.back;
				normals[j + stride] = normal;
				normals[j + stride * 2] = normal;
				normals[j + stride * 3] = normal;
				if (j < segments)
					rays[j + 1] = new Ray(vertices[j], radial);

				//segment triangles
				triangles[j * 3] = j + stride;
				triangles[j * 3 + 1] = j + stride * 2;
				triangles[j * 3 + 2] = j + stride * 3;

				//Base triangles
				if (j < segments - 2)
				{
					triangles[j * 3 + stride * 3] = 0;
					triangles[j * 3 + stride * 3 + 1] = j + 2;
					triangles[j * 3 + stride * 3 + 2] = j + 1;
				}
			}
			cone.vertices = vertices;
			cone.normals = normals;
			cone.triangles = triangles;
			//cone.RecalculateBounds();
			return cone;
		}
	}
}