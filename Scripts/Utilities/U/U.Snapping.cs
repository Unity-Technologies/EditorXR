namespace UnityEngine.VR.Utilities
{
	using UnityEngine;
	using System;
#if UNITY_EDITOR
	using UnityEditor.VR;
#endif

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public static partial class U
	{
		/// <summary>
		/// Snapping related EditorVR utilities
		/// </summary>
		public static class Snapping
		{

			private const float kSnapApproachDistance = .3f;
			private const float kSnapReleaseDistance = .5f;

			public static SnappingModes currentSnappingMode
			{
				get { return s_CurrentSnappingMode; }
				set { s_CurrentSnappingMode = value; }
			}
			private static SnappingModes s_CurrentSnappingMode = SnappingModes.SnapToGround;

			[Flags]
			public enum SnappingModes
			{
				None = 0,
				SnapToGround = 1,
				SnapToSurface = 2,
				SnapToSurfaceNormal = 4,
				Throw = 8
			}

			public static bool SnapToGroundPlane(Transform objectToSnap, Vector3 movement, Vector3 offset = default(Vector3))
			{
				Vector3 objectPosition = objectToSnap.position;
				bool movingAway = Mathf.Sign(objectPosition.y) == Mathf.Sign(movement.y);
				float snapDistance = movingAway ? kSnapReleaseDistance : kSnapApproachDistance;
				bool needSnap = Mathf.Abs(objectPosition.y + offset.y) < snapDistance;

				if (needSnap)
					objectPosition.y = -offset.y;

				objectToSnap.position = objectPosition;
				return needSnap;
			}

			public static bool SnapToSurface(Transform objectToSnap, Ray ray, float distance = float.PositiveInfinity, bool alignRotation = false)
			{
				RaycastHit hit;
				return SnapToSurface(objectToSnap, ray, out hit, distance, alignRotation);
			}

			public static bool SnapToSurface(Transform objectToSnap, Ray ray, out RaycastHit hit, float distance = float.PositiveInfinity, bool alignRotation = false)
			{
				bool hadTarget = GetRaySnapHit(ray, distance, out hit, objectToSnap);

				if (hadTarget)
				{
					if (alignRotation)
					{
						Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
						objectToSnap.rotation = rotation;
					}

					objectToSnap.position = hit.point;
				}

				return hadTarget;
			}

			public static bool GetRaySnapHit(Ray ray, float distance, out RaycastHit hit, params Transform[] raycastIgnore)
			{
				RaycastHit[] hits;
				int hitCount = GetRaycastHits(ray, distance, out hits);

				float closestDistance = distance;
				int closestIndex = -1;

				for (int i = 0; i < hitCount; i++)
				{
					if (hits[i].distance < closestDistance)
					{
						bool skip = IsIgnored(hits[i].transform, raycastIgnore);

						if (!skip)
						{
							closestIndex = i;
							closestDistance = hits[i].distance;
						}
					}
				}

				if (closestIndex > -1)
				{
					hit = hits[closestIndex];
					return true;
				}
				else
				{
					hit = default(RaycastHit);
					return false;
				}
			}

			private static int GetRaycastHits(Ray ray, float distance, out RaycastHit[] hits)
			{
				hits = new RaycastHit[10];
				int hitCount = Physics.RaycastNonAlloc(
					ray,
					hits,
					distance,
					VRView.viewerCamera.cullingMask,
					QueryTriggerInteraction.Ignore);

				return hitCount;
			}

			public static bool GetBoxSnapHit(Transform target, Ray ray, Vector3 extents, float distance, out RaycastHit hit, params Transform[] raycastIgnore)
			{
				RaycastHit[] hits;
				int hitCount = GetBoxcastHits(ray, extents, target.rotation, distance, out hits);
				
				Matrix4x4 localToWorld = target.localToWorldMatrix;
				var meshFilter = target.GetComponent<MeshFilter>();
				var mesh = meshFilter.sharedMesh;

				int closestIndex = GetClosestHit(hitCount, hits, distance, ray, raycastIgnore, mesh, localToWorld);

				if (closestIndex > -1)
				{
					hit = hits[closestIndex];
					return true;
				}
				else
				{
					hit = default(RaycastHit);
					return false;
				}
			}

			private static int GetClosestHit(int hitCount, RaycastHit[] hits, float distance, Ray ray, Transform[] raycastIgnore, Mesh mesh, Matrix4x4 localToWorld)
			{
				int closestIndex = -1;
				float closestDistance = distance;

				int vertexCount = mesh.vertexCount;
				Vector3[] vertices = mesh.vertices;

				for (int i = 0; i < hitCount; i++)
				{
					var rayHit = hits[i];
					if (rayHit.distance < closestDistance)
					{
						bool skip = IsIgnored(rayHit.transform, raycastIgnore);

						if (!skip && rayHit.distance < closestDistance)
						{
							float dot = Vector3.Dot(ray.direction, rayHit.normal);
							bool isTouching = rayHit.point == Vector3.zero && dot == -1;

							if (dot < 0 && !isTouching)
							{
								closestIndex = i;
								closestDistance = rayHit.distance;
							}
							else if (isTouching)
							{
								if (IsMovementValidOnTouch(ray, rayHit, localToWorld, vertices, vertexCount, distance))
								{
									closestIndex = i;
									closestDistance = rayHit.distance;
								}
							}
						}
					}
				}

				return closestIndex;
			}

			private static bool IsMovementValidOnTouch(Ray ray, RaycastHit rayHit, Matrix4x4 localToWorld, Vector3[] vertices, int vertexCount, float distance)
			{
				for (int v = 0; v < vertexCount; v++)
				{
					var worldVertex = localToWorld.MultiplyPoint(vertices[v]);

					Ray localRay = new Ray(worldVertex, ray.direction);
					localRay.origin = localRay.GetPoint(-distance);

					RaycastHit localHit;
					if (rayHit.collider.Raycast(localRay, out localHit, float.PositiveInfinity))
					{
						float localDot = Vector3.Dot(ray.direction, localHit.normal);
						if (localDot < 0)
						{
							return true;
						}
						break;
					}
				}

				return false;
			}

			private static int GetBoxcastHits(Ray ray, Vector3 extents, Quaternion rotation, float distance, out RaycastHit[] hits)
			{
				hits = new RaycastHit[10];
				int hitCount = Physics.BoxCastNonAlloc(
					ray.origin,
					extents,
					ray.direction,
					hits,
					rotation,
					distance,
					VRView.viewerCamera.cullingMask,
					QueryTriggerInteraction.Ignore);

				return hitCount;
			}

			private static bool IsIgnored(Transform objectToCheck, Transform[] ignoreList)
			{
				for (int i = 0; i < ignoreList.Length; i++)
				{
					if (ignoreList[i].Equals(objectToCheck))
						return true;
				}

				return false;
			}

			public static Vector3 GetClosestVertex(MeshFilter target, Vector3 surfacePoint, Vector3 surfaceNormal, bool singleAxisAwayFromSurface = true)
			{
				Vector3 closest = Vector3.zero;

				if (target)
				{
					var mesh = target.sharedMesh;
					if (mesh)
					{
						var vertexCount = mesh.vertexCount;
						var vertices = mesh.vertices;

						float lowestDistance = float.PositiveInfinity;

						Quaternion rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
						Matrix4x4 surfaceToWorld = Matrix4x4.TRS(surfacePoint, rotation, Vector3.one);
						Matrix4x4 worldToSurface = surfaceToWorld.inverse;
						Matrix4x4 objectToWorld = target.transform.localToWorldMatrix;

						for (int i = 0; i < vertexCount; i++)
						{
							var vertex = vertices[i];
							var transformVector = objectToWorld.MultiplyVector(vertex);
							var surfaceVertex = worldToSurface.MultiplyVector(transformVector);
							float vertexY = surfaceVertex.y;

							if (vertexY < lowestDistance)
							{
								if (singleAxisAwayFromSurface)
								{
									Vector3 onlyY = new Vector3(0, vertexY, 0);
									closest = surfaceToWorld.MultiplyVector(onlyY);
								}
								else
									closest = transformVector;

								lowestDistance = vertexY;
							}
						}
					}
				}

				return closest;
			}

			public static bool HasFlag(SnappingModes flag)
			{
				return (currentSnappingMode & flag) == flag;
			}

		}
	}
}
