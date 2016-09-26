namespace UnityEngine.VR.Utilities
{
	using UnityEngine;
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

			public enum SnappingModes
			{
				None = 0,
				SnapToGround = 1,
				SnapToSurface = 2,
				SnapToSurfaceNormal = 4,
				Throw = 8
			}

			public static bool SnapToGroundPlane(Transform objectToSnap, Vector3 movement)
			{
				Vector3 objectPosition = objectToSnap.position;
				bool movingAway = Mathf.Sign(objectPosition.y) == Mathf.Sign(movement.y);
				float snapDistance = movingAway ? kSnapReleaseDistance : kSnapApproachDistance;
				bool needSnap = Mathf.Abs(objectPosition.y) < snapDistance;

				if (needSnap)
					objectPosition.y = 0;

				objectToSnap.position = objectPosition;
				return needSnap;
			}

			public static bool SnapToSurface(Transform objectToSnap, Ray ray, float distance = float.PositiveInfinity, bool alignRotation = false)
			{
				RaycastHit hit;
				bool hadTarget = GetRaySnapHit(ray, distance, out hit, objectToSnap);

				if (hadTarget)
				{
					if (alignRotation)
						objectToSnap.up = hit.normal;

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

		}
	}
}
