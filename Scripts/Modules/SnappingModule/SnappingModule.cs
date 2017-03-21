using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SnappingModule : MonoBehaviour
	{
		const float k_GroundHeight = 0f;
		const float k_GroundSnapMin = 0.3f;
		const float k_GroundSnapMax = 0.5f;

		[SerializeField]
		GameObject m_GroundPlane;

		public bool groundSnapping { get; set; }

		readonly Dictionary<object, SnappingState> m_SnappingStates = new Dictionary<object, SnappingState>();

		class SnappingState
		{
			public bool groundSnapping;
			public Vector3 currentPosition;
		}

		void Awake()
		{
			m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
			m_GroundPlane.SetActive(false);
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

		public Vector3 TranslateWithSnapping(object caller, Vector3 currentPosition, Vector3 delta)
		{
			if (groundSnapping)
			{
				SnappingState state;
				if (!m_SnappingStates.TryGetValue(caller, out state))
				{
					state = new SnappingState { currentPosition = currentPosition};
					m_SnappingStates[caller] = state;
				}

				state.currentPosition += delta;

				var camera = CameraUtils.GetMainCamera();
				var position = state.currentPosition;
				var distToCamera = Mathf.Max(1, Mathf.Log(Vector3.Distance(camera.transform.position, position)));
				var diffGround = Mathf.Abs(position.y - k_GroundHeight);

				if (diffGround < k_GroundSnapMin * distToCamera)
					state.groundSnapping = true;

				if (diffGround > k_GroundSnapMax * distToCamera)
					state.groundSnapping = false;

				if (state.groundSnapping)
					position.y = k_GroundHeight;

				return position;
			}

			currentPosition += delta;
			return currentPosition;
		}

		public void ClearSnappingState(object caller)
		{
			m_SnappingStates.Remove(caller);
		}
	}
}
