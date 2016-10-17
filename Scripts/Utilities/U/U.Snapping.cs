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
			private static SnappingModes s_CurrentSnappingMode = SnappingModes.SnapToGround | SnappingModes.SnapToSurfaceNormal;

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
				RaycastHit[] hits = new RaycastHit[10];
				int hitCount = Physics.RaycastNonAlloc(
					ray,
					hits,
					distance,
					VRView.viewerCamera.cullingMask,
					QueryTriggerInteraction.Ignore);

				float closestDistance = distance;
				int closestIndex = -1;

				for (int i = 0; i < hitCount; i++)
				{
					if (hits[i].distance < closestDistance)
					{
						bool skip = false;

						for (int j = 0; j < raycastIgnore.Length; j++)
						{
							if (raycastIgnore[j].Equals(hits[i].collider.transform))
							{
								skip = true;
								break;
							}
						}

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

			public static bool GetBoxSnapHit(Quaternion rotation, Ray ray, Bounds bounds, float distance, out RaycastHit hit, params Transform[] raycastIgnore)
			{
				RaycastHit[] hits = new RaycastHit[10];
				int hitCount = Physics.BoxCastNonAlloc(
					ray.origin + bounds.center,
					bounds.extents,
					ray.direction,
					hits,
					rotation,
					distance,
					VRView.viewerCamera.cullingMask,
					QueryTriggerInteraction.Ignore);

				float closestDistance = distance;
				int closestIndex = -1;

				for (int i = 0; i < hitCount; i++)
				{
					if (hits[i].distance < closestDistance)
					{
						bool skip = false;

						for (int j = 0; j < raycastIgnore.Length; j++)
						{
							if (raycastIgnore[j].Equals(hits[i].collider.transform))
							{
								skip = true;
								break;
							}
						}

						if (!skip)
						{
							float directionDot = Vector3.Dot(ray.direction, hits[i].point - ray.origin);
							if (directionDot > 0)
							{
								closestIndex = i;
								closestDistance = hits[i].distance;
							}
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
