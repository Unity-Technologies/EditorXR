namespace UnityEngine.VR.Utilities
{
	using System;
	using UnityEngine;
#if UNITY_EDITOR
	using UnityEditor;
	using System.Reflection;
	using Modules;
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

			public static void SnapToGroundPlane(Transform objectToSnap, Vector3 movement)
			{
				Vector3 objectPosition = objectToSnap.position;
				bool movingAway = Mathf.Sign(objectPosition.y) == Mathf.Sign(movement.y);
				float snapDistance = movingAway ? kSnapReleaseDistance : kSnapApproachDistance;

				if (Mathf.Abs(objectPosition.y) < snapDistance)
					objectPosition.y = 0;

				objectToSnap.position = objectPosition;
			}

			public static void SnapToSurface(Transform objectToSnap, Ray ray, bool alignRotation = false)
			{
				RaycastHit hit;
				bool hadTarget = GetRaySnapHit(ray, out hit, objectToSnap);

				if (hadTarget)
				{
					if (alignRotation)
						objectToSnap.up = hit.normal;

					objectToSnap.position = hit.point;
				}
			}

			private static bool GetRaySnapHit(Ray ray, out RaycastHit hit, params Transform[] raycastIgnore)
			{
				RaycastHit[] hits = new RaycastHit[10];
				int hitCount = Physics.RaycastNonAlloc(ray,
					hits,
					float.PositiveInfinity,
					VRView.viewerCamera.cullingMask,
					QueryTriggerInteraction.Ignore);

				float closestDistance = float.PositiveInfinity;
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
