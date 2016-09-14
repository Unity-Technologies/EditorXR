using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Utilities
{
	public static partial class U
	{
		public static class Intersection
		{
			public static bool TestObject(MeshCollider collisionTester, Renderer obj, IntersectionTester tester)
			{
				for (int j = 0; j < tester.rays.Length; j++)
				{
					Ray ray = tester.rays[j];
					
					//Transform to world space
					ray.origin = tester.transform.TransformPoint(ray.origin);
					ray.direction = tester.transform.TransformDirection(ray.direction);

					if (TestRay(collisionTester, obj, ray))
						return true;
				}

				return false;
			}

			public static bool TestRay(MeshCollider collisionTester, Renderer obj, Ray ray)
			{
				var mf = obj.GetComponent<MeshFilter>();
				
				collisionTester.sharedMesh = mf.sharedMesh;

				ray.origin = mf.transform.InverseTransformPoint(ray.origin);
				ray.direction = mf.transform.InverseTransformDirection(ray.direction);
		
				float maxDistance = collisionTester.bounds.size.magnitude;
				RaycastHit hitInfo;

				// Shoot a ray from outside the object (due to face normals) in the direction of the ray to see if it is inside
				Ray forwardRay = new Ray(ray.origin, ray.direction);
				forwardRay.origin = forwardRay.GetPoint(-maxDistance);
				Vector3 forwardHit;
				if (collisionTester.Raycast(forwardRay, out hitInfo, maxDistance*2f))
					forwardHit = hitInfo.point;
				else
					return false;
				
				// Shoot a ray in the other direction, too, from outside the object (due to face normals)
				Vector3 behindHit;
				Ray behindRay = new Ray(ray.origin, -ray.direction);
				ray.origin = ray.GetPoint(-maxDistance);
				if (collisionTester.Raycast(behindRay, out hitInfo, maxDistance*2f))
					behindHit = hitInfo.point;
				else
					return false;

				// Check whether point (i.e. ray origin) is contained within the object
				float projection = Vector3.Dot(forwardHit - behindHit, ray.origin - behindHit);
				return (projection >= 0f && projection <= 1f);
			}

			public static Vector3 InvertVector3(Vector3 vec)
			{
				return new Vector3(1 / vec.x, 1 / vec.y, 1 / vec.z);
			}

			//from Real Time Collision Detection p.141
			public static Vector3 ClosestPtPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
			{
				float v, w;
				Vector3 ab = b - a;
				Vector3 ac = c - a;
				Vector3 ap = p - a;
				float d1 = Vector3.Dot(ab, ap);
				float d2 = Vector3.Dot(ac, ap);

				if (d1 <= 0.0f && d2 <= 0.0f) return a; // barycentric coordinates (1,0,0)

				// Check if p in vertex region outside B
				Vector3 bp = p - b;
				float d3 = Vector3.Dot(ab, bp);
				float d4 = Vector3.Dot(ac, bp);
				if (d3 >= 0.0f && d4 <= d3) return b; // barycentric coordinates (0,1,0)

				// Check if p in edge region of AB, if so return projection of P onto AB
				float vc = d1 * d4 - d3 * d2;
				if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
				{
					v = d1 / (d1 - d3);
					return a + v * ab; // barycentric coordinates (1-v,v,0)
				}

				Vector3 cp = p - c;
				float d5 = Vector3.Dot(ab, cp);
				float d6 = Vector3.Dot(ac, cp);
				if (d6 >= 0.0f && d5 <= d6) return c; // barycentric coordinates (0,0,1)

				// Check if P in edge region of AC, if os return projection of P onto AC
				float vb = d5 * d2 - d1 * d6;
				if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
				{
					w = d2 / (d2 - d6);
					return a + w * ac; // barycentric coordinates (1-w,0,w)
				}

				// Check if P in edge region of BC, if so return projection of P onto BC
				float va = d3 * d6 - d5 * d4;
				if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
				{
					w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
					return b + w * (c - b); // barycentric coordinates (0,1-w,w)
				}

				// P inside face region. Compute Q through its barycentric coordinates (u,v,w)
				float denom = 1.0f / (va + vb + vc);
				v = vb * denom;
				w = vc * denom;
				return a + ab * v + ac * w; // = u*a + v*b + w*c, u = va * denom = 1.0f - v - w
			}

			// From Real Time Collision Detection p.167
			// Returns true if sphere intersects triangle ABC, false otherwise.
			// Point is the point on abc closest to the sphere center
			public static bool TestSphereTriangle(Vector3 center, float radius, Vector3 a, Vector3 b, Vector3 c, out Vector3 point)
			{
				// Find point on triangle ABC closest to sphere center
				point = ClosestPtPointTriangle(center, a, b, c);
				// Sphere and triangle intersect if the (squared) distance from the sphere
				// center to point p is less than the (squared) sphere radius
				Vector3 v = point - center;
				return Vector3.Dot(v, v) <= radius * radius;
			}

			// From Real Time Collision Detection p.191
			public static bool IntersectSegmentTriangle(Vector3 p, Vector3 q, Vector3 a, Vector3 b, Vector3 c, out float u, out float v, out float w, out float t)
			{
				u = v = w = t = 0;
				Vector3 ab = b - a;
				Vector3 ac = c - a;
				Vector3 qp = p - q;

				Vector3 n = Vector3.Cross(ab, ac);
				float d = Vector3.Dot(qp, n);
				if (d <= 0.0f) return false;

				Vector3 ap = p - a;
				t = Vector3.Dot(ap, n);
				if (t < 0.0f) return false;
				if (t > d) return false;

				Vector3 e = Vector3.Cross(qp, ap);
				v = Vector3.Dot(ac, e);
				if (v < 0.0f || v > d) return false;
				w = -Vector3.Dot(ab, e);
				if (w < 0.0f || v + w > d) return false;

				float ood = 1.0f / d;
				t *= ood;
				v *= ood;
				w *= ood;
				u = 1.0f - v - w;
				return true;
			}

			public static bool IntersectRayAABB(Ray ray, Vector3 min, Vector3 max, float tMin, float tMax)
			{
				for (int i = 0; i < 3; i++)
				{
					float invD = 1.0f / ray.direction[i];
					float t0 = (min[i] - ray.origin[i]) * invD;
					float t1 = (max[i] - ray.origin[i]) * invD;
					if (invD < 0.0f)
						Swap(ref t0, ref t1);
					tMin = t0 > tMin ? t0 : tMin;
					tMax = t1 < tMax ? t1 : tMax;
					if (tMax <= tMin)
						return false;
				}
				return true;
			}

			public static void Swap<T>(ref T a, ref T b)
			{
				T t = a;
				a = b;
				b = t;
			}

			public static bool TestTriangleAABB(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 min, Vector3 max)
			{
				Bounds b = new Bounds();
				b.min = min;
				b.max = max;
				return TestTriangleAABB(v0, v1, v2, b);
			}

			// From http://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/code/tribox3.txt (translated from C)
			public static bool TestTriangleAABB(Vector3 v0, Vector3 v1, Vector3 v2, Bounds b)
			{
				/*    use separating axis theorem to test overlap between triangle and box */
				/*    need to test for overlap in these directions: */
				/*    1) the {x,y,z}-directions (actually, since we use the AABB of the triangle */
				/*       we do not even need to test these) */
				/*    2) normal of the triangle */
				/*    3) crossproduct(edge from tri, {x,y,z}-directin) */
				/*       this gives 3x3=9 more tests */

				float min, max, p0, p1, p2, rad, fex, fey, fez;

				/* This is the fastest branch on Sun */
				/* move everything so that the boxcenter is in (0,0,0) */
				v0 -= b.center;
				v1 -= b.center;
				v2 -= b.center;

				//tribox3.text from here on
				Vector3 e0 = v1 - v0, e1 = v2 - v1, e2 = v0 - v2;

				//TODO: inlining?
				fex = Mathf.Abs(e0.x);
				fey = Mathf.Abs(e0.y);
				fez = Mathf.Abs(e0.z);
				//AXISTEST_X01(e0[Z], e0[Y], fez, fey);
				p0 = e0.z * v0.y - e0.y * v0.z;
				p2 = e0.z * v2.y - e0.y * v2.z;
				if (p0 < p2)
				{
					min = p0;
					max = p2;
				} else
				{
					min = p2;
					max = p0;
				}
				rad = fez * b.extents.y + fey * b.extents.z;
				if (min > rad || max < -rad) return false;

				//AXISTEST_Y02(e0[Z], e0[X], fez, fex);
				p0 = -e0.z * v0.x + e0.x * v0.z;
				p2 = -e0.z * v2.x + e0.x * v2.z;
				if (p0 < p2)
				{
					min = p0;
					max = p2;
				} else
				{
					min = p2;
					max = p0;
				}
				rad = fez * b.extents.x + fex * b.extents.z;
				if (min > rad || max < -rad) return false;

				//AXISTEST_Z12(e0[Y], e0[X], fey, fex);
				p1 = e0.y * v1.x - e0.x * v1.y;
				p2 = e0.y * v2.x - e0.x * v2.y;
				if (p2 < p1)
				{
					min = p2;
					max = p1;
				} else
				{
					min = p1;
					max = p2;
				}
				rad = fey * b.extents.x + fex * b.extents.y;
				if (min > rad || max < -rad) return false;

				fex = Mathf.Abs(e1.x);
				fey = Mathf.Abs(e1.y);
				fez = Mathf.Abs(e1.z);
				//AXISTEST_X01(e1[Z], e1[Y], fez, fey);
				p0 = e1.z * v0.y - e1.y * v0.z;
				p2 = e1.z * v2.y - e1.y * v2.z;
				if (p0 < p2)
				{
					min = p0;
					max = p2;
				} else
				{
					min = p2;
					max = p0;
				}
				rad = fez * b.extents.y + fey * b.extents.z;
				if (min > rad || max < -rad) return false;

				//AXISTEST_Y02(e1[Z], e1[X], fez, fex);
				p0 = -e1.z * v0.x + e1.x * v0.z;
				p2 = -e1.z * v2.x + e1.x * v2.z;
				if (p0 < p2)
				{
					min = p0;
					max = p2;
				} else
				{
					min = p2;
					max = p0;
				}
				rad = fez * b.extents.x + fex * b.extents.z;
				if (min > rad || max < -rad) return false;

				//AXISTEST_Z0(e1[Y], e1[X], fey, fex);
				p0 = e1.y * v0.x - e1.x * v0.y;
				p1 = e1.y * v1.x - e1.x * v1.y;
				if (p0 < p1)
				{
					min = p0;
					max = p1;
				} else
				{
					min = p1;
					max = p0;
				}
				rad = fey * b.extents.x + fex * b.extents.y;
				if (min > rad || max < -rad) return false;

				fex = Mathf.Abs(e2.x);
				fey = Mathf.Abs(e2.y);
				fez = Mathf.Abs(e2.z);

				//AXISTEST_X2(e2[Z], e2[Y], fez, fey);             
				p0 = e2.z * v0.y - e2.y * v0.z;
				p1 = e2.z * v1.y - e2.y * v1.z;
				if (p0 < p1)
				{
					min = p0;
					max = p1;
				} else
				{
					min = p1;
					max = p0;
				}

				rad = fez * b.extents.y + fey * b.extents.z;
				if (min > rad || max < -rad) return false;

				//AXISTEST_Y1(e2[Z], e2[X], fez, fex); 
				p0 = -e2.z * v0.x + e2.x * v0.z;
				p1 = -e2.z * v1.x + e2.x * v1.z;
				if (p0 < p1)
				{
					min = p0;
					max = p1;
				} else
				{
					min = p1;
					max = p0;
				}
				rad = fez * b.extents.x + fex * b.extents.z;
				if (min > rad || max < -rad) return false;

				//AXISTEST_Z12(e2[Y], e2[X], fey, fex);        
				p1 = e2.y * v1.x - e2.x * v1.y;
				p2 = e2.y * v2.x - e2.x * v2.y;
				if (p2 < p1)
				{
					min = p2;
					max = p1;
				} else
				{
					min = p1;
					max = p2;
				}
				rad = fey * b.extents.x + fex * b.extents.y;
				if (min > rad || max < -rad) return false;

				/* Bullet 1: */
				/*  first test overlap in the {x,y,z}-directions */
				/*  find min, max of the triangle each direction, and test for overlap in */
				/*  that direction -- this is equivalent to testing a minimal AABB around */
				/*  the triangle against the AABB */

				/* test in X-direction */
				//FINDMINMAX(v0[X], v1[X], v2[X], min, max);
				min = max = v0.x;
				if (v1.x < min) min = v1.x;
				if (v1.x > max) max = v1.x;
				if (v2.x < min) min = v2.x;
				if (v2.x > max) max = v2.x;
				if (min > b.extents.x || max < -b.extents.x) return false;

				/* test in Y-direction */
				//FINDMINMAX(v0[Y], v1[Y], v2[Y], min, max);
				min = max = v0.y;
				if (v1.y < min) min = v1.y;
				if (v1.y > max) max = v1.y;
				if (v2.y < min) min = v2.y;
				if (v2.y > max) max = v2.y;
				if (min > b.extents.y || max < -b.extents.y) return false;

				/* test in Z-direction */
				//FINDMINMAX(v0[Z], v1[Z], v2[Z], min, max);
				min = max = v0.y;
				if (v1.z < min) min = v1.z;
				if (v1.z > max) max = v1.z;
				if (v2.z < min) min = v2.z;
				if (v2.z > max) max = v2.z;
				if (min > b.extents.z || max < -b.extents.z) return false;

				/* Bullet 2: */
				/*  test if the box intersects the plane of the triangle */
				/*  compute plane equation of triangle: normal*x+d=0 */
				Vector3 normal = Vector3.Cross(e0, e1);
				// -NJMP- (line removed here)

				if (!PlaneBoxOverlap(normal, v0, b.extents)) return false; // -NJMP-

				return true; /* box and triangle overlaps */
			}

			// From http://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/code/tribox3.txt (translated from C)
			public static bool PlaneBoxOverlap(Vector3 normal, Vector3 vert, Vector3 maxBox)
			{
				int q;
				Vector3 vmin = Vector3.zero;
				Vector3 vmax = Vector3.zero;
				float v;

				for (q = 0; q <= 2; q++)
				{
					v = vert[q]; // -NJMP-  
					if (normal[q] > 0.0f)
					{
						vmin[q] = -maxBox[q] - v; // -NJMP-  
						vmax[q] = maxBox[q] - v; // -NJMP-  
					} else
					{
						vmin[q] = maxBox[q] - v; // -NJMP-  
						vmax[q] = -maxBox[q] - v; // -NJMP-  
					}
				}
				if (Vector3.Dot(normal, vmin) > 0.0f) return false; // -NJMP-
				if (Vector3.Dot(normal, vmax) >= 0.0f) return true; // -NJMP-

				return false;
			}
		}
	}
}