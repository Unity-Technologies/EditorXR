using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SnappingModule : MonoBehaviour
	{
		const float k_MaxRayLength = 100f;

		const float k_GroundHeight = 0f;
		const float k_GroundSnapMin = 0.3f;
		const float k_GroundSnapMax = 0.5f;

		const float k_FaceSnapBreakDist = 0.5f;

		[SerializeField]
		GameObject m_GroundPlane;

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

		readonly Dictionary<Transform, SnappingState> m_SnappingStates = new Dictionary<Transform, SnappingState>();

		class SnappingState
		{
			public Vector3 currentPosition;
			public Bounds localBounds;
			public bool groundSnapping;
			public bool faceSnapping;
			public Vector3 faceSnappingStartPosition;
		}

		void Awake()
		{
			m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
			m_GroundPlane.SetActive(false);

			groundSnapping = true;
			faceSnapping = true;
		}

		void Update()
		{
			if (groundSnapping)
			{
				var shouldActivateGroundPlane = false;
				foreach (var state in m_SnappingStates.Values)
				{
					if (state.groundSnapping)
						shouldActivateGroundPlane = true;
				}
				m_GroundPlane.SetActive(shouldActivateGroundPlane);
			}
		}

		public void TranslateWithSnapping(Transform rayOrigin, GameObject[] objects, ref Vector3 position, ref Quaternion rotation, Vector3 delta, bool constrained)
		{
			if (snappingEnabled)
			{
				SnappingState state;
				if (!m_SnappingStates.TryGetValue(rayOrigin, out state))
				{
					var totalBounds = ObjectUtils.GetBounds(objects);
					totalBounds.center -= position;
					state = new SnappingState
					{
						currentPosition = position,
						localBounds = totalBounds
					};
					m_SnappingStates[rayOrigin] = state;
				}

				state.currentPosition += delta;
				var statePosition = state.currentPosition;

				var camera = CameraUtils.GetMainCamera();
					var distToCamera = Mathf.Max(1, Mathf.Log(Vector3.Distance(camera.transform.position, statePosition)));

				if (faceSnapping && !constrained)
				{
					var ray = new Ray(rayOrigin.position, rayOrigin.forward);
					RaycastHit hit;
					if (raycast(ray, out hit, k_MaxRayLength))
					{
						state.faceSnapping = true;
						rotation = Quaternion.LookRotation(hit.normal) * Quaternion.AngleAxis(90, Vector3.right);
						position = hit.point;
						state.faceSnappingStartPosition = position;
						return;
					}

					if (state.faceSnapping)
					{
						var faceSnapBreakDist = k_FaceSnapBreakDist * distToCamera;
						if (Vector3.Distance(state.faceSnappingStartPosition, statePosition) > faceSnapBreakDist)
						{
							position = statePosition;
							state.faceSnapping = false;
						}
						return;
					}
				}

				if (groundSnapping)
				{
					var diffGround = Mathf.Abs(statePosition.y - k_GroundHeight);

					var groundSnapMin = k_GroundSnapMin * distToCamera;
					var groundSnapMax = k_GroundSnapMax * distToCamera;

					var bounds = state.localBounds;
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


						return;
					}
				}
			}

			position += delta;
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
