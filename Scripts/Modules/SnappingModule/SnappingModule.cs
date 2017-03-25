using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SnappingModule : MonoBehaviour, IUsesViewerScale
	{
		const float k_MaxRayLength = 100f;

		const float k_GroundHeight = 0f;
		const float k_GroundSnapMin = 0.3f;
		const float k_GroundSnapMax = 0.5f;

		const float k_FaceSnapBreakDist = 0.5f;

		const float k_WidgetScale = 0.03f;

		[SerializeField]
		GameObject m_GroundPlane;

		[SerializeField]
		GameObject m_Widget;

		public RaycastDelegate raycast { private get; set; }

		public bool snappingEnabled
		{
			get { return !m_DisableAll && (groundSnapping || faceSnapping); }
			set
			{
				Reset();
				m_DisableAll = value;
			}
		}
		bool m_DisableAll;

		// Snapping Modes
		public bool groundSnapping
		{
			get {return m_GroundSnapping; }
			set
			{
				if (value == m_GroundSnapping)
					return;

				Reset();
				m_GroundSnapping = value;
			}
		}
		bool m_GroundSnapping;

		public bool faceSnapping
		{
			get {return m_FaceSnapping; }
			set
			{
				if (value == m_FaceSnapping)
					return;

				Reset();
				m_FaceSnapping = value;
			}
		}
		bool m_FaceSnapping;

		// Modifiers
		public bool pivotSnapping { get; set; }
		public bool snapRotation { get; set; }

		readonly Dictionary<Transform, SnappingState> m_SnappingStates = new Dictionary<Transform, SnappingState>();

		public Func<float> getViewerScale { get; set; }

		class SnappingState
		{
			public Vector3 currentPosition;
			public Quaternion currentRotation;
			public Bounds rotatedBounds;
			public Bounds identityBounds;
			public bool groundSnapping;
			public bool faceSnapping;
			public Vector3 faceSnappingStartPosition;
			public Quaternion faceSnappingRotation;
			public GameObject[] objects;
		}

		void Awake()
		{
			m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
			m_GroundPlane.SetActive(false);

			m_Widget = ObjectUtils.Instantiate(m_Widget, transform);
			m_Widget.SetActive(false);

			groundSnapping = true;
			faceSnapping = true;
		}

		void Update()
		{
			if (snappingEnabled)
			{
				SnappingState faceSnapping = null;
				var shouldActivateGroundPlane = false;
				foreach (var state in m_SnappingStates.Values)
				{
					if (state.groundSnapping)
						shouldActivateGroundPlane = true;

					if (state.faceSnapping)
						faceSnapping = state;
				}
				m_GroundPlane.SetActive(shouldActivateGroundPlane);

				var shouldActivateWidget = faceSnapping != null;
				m_Widget.SetActive(shouldActivateWidget);
				if (shouldActivateWidget)
				{
					var statePosition = faceSnapping.faceSnappingStartPosition;
					var camera = CameraUtils.GetMainCamera();
					var distToCamera = Vector3.Distance(camera.transform.position, statePosition);
					m_Widget.transform.position = statePosition;
					m_Widget.transform.rotation = faceSnapping.faceSnappingRotation;
					m_Widget.transform.localScale = Vector3.one * k_WidgetScale * distToCamera;
				}
			}
		}

		public bool TranslateWithSnapping(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained)
		{
			if (snappingEnabled)
			{
				var state = GetSnappingState(rayOrigin, objects, position, rotation);

				state.currentPosition += delta;
				var statePosition = state.currentPosition;

				var camera = CameraUtils.GetMainCamera();
				var distToCamera = Mathf.Max(1, Mathf.Log(Vector3.Distance(camera.transform.position, statePosition)));

				if (faceSnapping && !constrained)
				{
					var ray = new Ray(rayOrigin.position, rayOrigin.forward);
					if (PerformFaceSnapping(ray, ref position, ref rotation, statePosition, state, Vector3.down, rotation, distToCamera))
						return true;
				}

				if (groundSnapping && PerformGroundSnapping(ref position, ref rotation, statePosition, state, distToCamera))
					return true;
			}

			position += delta;

			return false;
		}

		public bool DirectTransformWithSnapping(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
		{
			if (snappingEnabled)
			{
				var state = GetSnappingState(rayOrigin, objects, position, rotation);

				state.currentPosition = targetPosition;

				if (faceSnapping)
				{
					var bounds = state.identityBounds;
					var offset = bounds.center;
					var directions = new[] { Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back, Vector3.up };
					foreach (var direction in directions)
					{
						var ray = new Ray(targetPosition + targetRotation * offset, targetRotation * direction);
						var raycastDistance = state.identityBounds.extents.y;
						if (PerformFaceSnapping(ray, ref position, ref rotation, targetPosition, state, direction, targetRotation, Mathf.Log(getViewerScale()), raycastDistance))
							return true;
					}
				}

				if (groundSnapping && PerformGroundSnapping(ref position, ref rotation, targetPosition, state, Mathf.Log(getViewerScale())))
					return true;
			}

			position = targetPosition;
			rotation = targetRotation;

			return false;
		}

		bool PerformFaceSnapping(Ray ray, ref Vector3 position, ref Quaternion rotation, Vector3 statePosition, SnappingState state, Vector3 direction, Quaternion targetRotation, float breakScale, float raycastDistance = k_MaxRayLength)
		{
			RaycastHit hit;
			GameObject go;
			if (raycast(ray, out hit, out go, raycastDistance, state.objects))
			{
				state.faceSnapping = true;
				state.groundSnapping = false;

				var snappedRotation = Quaternion.LookRotation(hit.normal) * Quaternion.AngleAxis(90, Vector3.right);

				if (snapRotation)
					rotation = snappedRotation;
				else
					rotation = targetRotation;

				if (pivotSnapping)
				{
					position = hit.point;
				}
				else
				{
					var bounds = state.identityBounds;
					var offset = bounds.center.y - bounds.extents.y;
					position = hit.point + rotation * direction * offset;
				}

				state.faceSnappingStartPosition = position;
				state.faceSnappingRotation = snappedRotation;
				return true;
			}

			if (state.faceSnapping)
			{
				var faceSnapBreakDist = k_FaceSnapBreakDist * breakScale;
				if (Vector3.Distance(state.faceSnappingStartPosition, statePosition) > faceSnapBreakDist)
				{
					position = statePosition;
					state.faceSnapping = false;
				}
				return true;
			}

			return false;
		}


		bool PerformGroundSnapping(ref Vector3 position, ref Quaternion rotation, Vector3 statePosition, SnappingState state, float breakScale)
		{
			if(groundSnapping)
			{
				var diffGround = Mathf.Abs(statePosition.y - k_GroundHeight);

				var groundSnapMin = k_GroundSnapMin * breakScale;
				var groundSnapMax = k_GroundSnapMax * breakScale;

				var bounds = state.rotatedBounds;
				var offset = bounds.center.y - bounds.extents.y;

				if (!pivotSnapping)
					diffGround = Mathf.Abs(statePosition.y + offset - k_GroundHeight);

				if (diffGround < groundSnapMin)
					state.groundSnapping = true;

				if (diffGround > groundSnapMax)
					state.groundSnapping = false;

				if (state.groundSnapping)
				{
					if (pivotSnapping)
						statePosition.y = k_GroundHeight;
					else
						statePosition.y = k_GroundHeight - offset;

					position = statePosition;
					
					if (snapRotation)
						rotation = Quaternion.identity;

					return true;
				}
			}

			return false;
		}

		SnappingState GetSnappingState(Transform rayOrigin, GameObject[] objects, Vector3 position, Quaternion rotation)
		{
			SnappingState state;
			if (!m_SnappingStates.TryGetValue(rayOrigin, out state))
			{
				float angle;
				Vector3 axis;
				rotation.ToAngleAxis(out angle, out axis);
				foreach (var go in objects)
				{
					go.transform.RotateAround(position, axis, -angle);
				}

				var identityBounds = ObjectUtils.GetBounds(objects);

				foreach (var go in objects)
				{
					go.transform.RotateAround(position, axis, angle);
				}

				var totalBounds = ObjectUtils.GetBounds(objects);
				totalBounds.center -= position;
				identityBounds.center -= position;
				state = new SnappingState
				{
					currentPosition = position,
					currentRotation = rotation,
					rotatedBounds = totalBounds,
					identityBounds = identityBounds,
					objects = objects
				};
				m_SnappingStates[rayOrigin] = state;
			}
			return state;
		}

		public void ClearSnappingState(Transform rayOrigin)
		{
			m_SnappingStates.Remove(rayOrigin);
		}

		void Reset()
		{
			m_SnappingStates.Clear();
		}
	}
}
