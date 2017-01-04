using UnityEngine.Experimental.EditorVR.Modules;

namespace UnityEngine.Experimental.EditorVR.Utilities
{
	public static partial class U
	{
		public static class Intersection
		{
			/// <summary>
			/// Test whether an object collides with the tester
			/// </summary>
			/// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
			/// <param name="obj">The object to test collision on</param>
			/// <param name="tester">The tester object</param>
			/// <returns>The result of whether the tester is in intersection with or located within the object</returns>
			public static bool TestObject(MeshCollider collisionTester, Renderer obj, IntersectionTester tester)
			{
				var transform = obj.transform;

				// Try a simple test with specific rays located at vertices
				for (var j = 0; j < tester.rays.Length; j++)
				{
					var ray = tester.rays[j];

					//Transform rays to world space
					var testerTransform = tester.transform;
					ray.origin = testerTransform.TransformPoint(ray.origin);
					ray.direction = testerTransform.TransformDirection(ray.direction);

					if (TestRay(collisionTester, transform, ray))
						return true;
				}

				// Try a more robust version with all edges
				return TestEdges(collisionTester, transform, tester);
			}

			/// <summary>
			/// Test the edges of the tester's collider against another mesh collider for intersection of being contained within
			/// </summary>
			/// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
			/// <param name="obj">The object to test collision on</param>
			/// <param name="tester">The tester object</param>
			/// <returns>The result of whether the point/ray is intersection with or located within the object</returns>
			public static bool TestEdges(MeshCollider collisionTester, Transform obj, IntersectionTester tester)
			{
				var mf = obj.GetComponent<MeshFilter>();
				if (mf)
					collisionTester.sharedMesh = mf.sharedMesh;

				var triangles = tester.triangles;
				var vertices = tester.vertices;

				float maxDistance = collisionTester.bounds.size.magnitude;

				var triangleVertices = new Vector3[3];
				var testerTransform = tester.transform;
				for (int i = 0; i < triangles.Length; i += 3)
				{
					triangleVertices[0] = vertices[triangles[i]];
					triangleVertices[1] = vertices[triangles[i + 1]];
					triangleVertices[2] = vertices[triangles[i + 2]];

					for (int j = 0; j < 3; j++)
					{
						RaycastHit hitInfo;

						var start = obj.InverseTransformPoint(testerTransform.TransformPoint(triangleVertices[j]));
						var end = obj.InverseTransformPoint(testerTransform.TransformPoint(triangleVertices[(j + 1) % 3]));
						var direction = (end - start).normalized;

						// Handle degenerate triangles
						if (Mathf.Approximately(direction.magnitude, 0f))
							continue;

						// Shoot a ray from outside the object (due to face normals) in the direction of the ray to see if it is inside
						var forwardRay = new Ray(start, direction);
						forwardRay.origin = forwardRay.GetPoint(-maxDistance);

						Vector3 forwardHit;

						if (collisionTester.Raycast(forwardRay, out hitInfo, maxDistance * 2f))
							forwardHit = hitInfo.point;
						else
							continue;

						// Shoot a ray in the other direction, too, from outside the object (due to face normals)
						Vector3 behindHit;
						var behindRay = new Ray(end, -direction);
						behindRay.origin = behindRay.GetPoint(-maxDistance);
						if (collisionTester.Raycast(behindRay, out hitInfo, maxDistance * 2f))
							behindHit = hitInfo.point;
						else
							continue;

						// Check whether the triangle edge is contained or intersects with the object
						var A = forwardHit;
						var B = behindHit;
						var C = start;
						var D = end;
						if (OnSegment(A, C, B)
							|| OnSegment(A, D, B)
							|| OnSegment(C, A, D)
							|| OnSegment(C, B, D))
						{
							return true;
						}
					}
				}

				return false;
			}

			/// <summary>
			/// Returns whether C lies on segment AB
			/// </summary>
			public static bool OnSegment(Vector3 A, Vector3 C, Vector3 B)
			{
				return Mathf.Approximately(Vector3.Distance(A, C) + Vector3.Distance(C, B), Vector3.Distance(A, B));
			}

			/// <summary>
			/// Tests a "ray" against a collider; Really we are testing whether a point is located within or is intersecting with a collider
			/// </summary>
			/// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
			/// <param name="obj">The object to test collision on</param>
			/// <param name="ray">A ray positioned at a vertex of the tester's collider</param>
			/// <returns>The result of whether the point/ray is intersection with or located within the object</returns>
			public static bool TestRay(MeshCollider collisionTester, Transform obj, Ray ray)
			{
				var mf = obj.GetComponent<MeshFilter>();
				if (mf)
					collisionTester.sharedMesh = mf.sharedMesh;

				ray.origin = obj.InverseTransformPoint(ray.origin);
				ray.direction = obj.InverseTransformDirection(ray.direction);
		
				var boundsSize = collisionTester.bounds.size.magnitude;
				var maxDistance = boundsSize * 2f;
				RaycastHit hitInfo;

				// Shoot a ray from outside the object (due to face normals) in the direction of the ray to see if it is inside
				var forwardRay = new Ray(ray.origin, ray.direction);
				forwardRay.origin = forwardRay.GetPoint(-boundsSize);

				Vector3 forwardHit;
				if (collisionTester.Raycast(forwardRay, out hitInfo, maxDistance))
					forwardHit = hitInfo.point;
				else
					return false;
				
				// Shoot a ray in the other direction, too, from outside the object (due to face normals)
				Vector3 behindHit;
				var behindRay = new Ray(ray.origin, -ray.direction);
				behindRay.origin = behindRay.GetPoint(-boundsSize);
				if (collisionTester.Raycast(behindRay, out hitInfo, maxDistance))
					behindHit = hitInfo.point;
				else
					return false;

				// Check whether the point (i.e. ray origin) is contained within the object
				var collisionLine = forwardHit - behindHit;
				var projection = Vector3.Dot(collisionLine, ray.origin - behindHit);
				return projection >= 0f && projection <= collisionLine.sqrMagnitude;
			}
		}
	}
}
