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

		public bool pivotSnapping { get; set; }
		public bool groundSnapping { get; set; }

		readonly Dictionary<object, SnappingState> m_SnappingStates = new Dictionary<object, SnappingState>();

		class SnappingState
		{
			public bool groundSnapping;
			public Vector3 currentPosition;
			public Bounds localBounds;
		}

		void Awake()
		{
			m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
			m_GroundPlane.SetActive(false);

			pivotSnapping = true;
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

		public Vector3 TranslateWithSnapping(object caller, GameObject[] objects, Vector3 position, Vector3 delta)
		{
			if (groundSnapping)
			{
				SnappingState state;
				if (!m_SnappingStates.TryGetValue(caller, out state))
				{
					var totalBounds = ObjectUtils.GetBounds(objects);
					totalBounds.center -= position;
					state = new SnappingState
					{
						currentPosition = position,
						localBounds = totalBounds
					};
					m_SnappingStates[caller] = state;
				}

				state.currentPosition += delta;

				var camera = CameraUtils.GetMainCamera();
				position = state.currentPosition;
				var distToCamera = Mathf.Max(1, Mathf.Log(Vector3.Distance(camera.transform.position, position)));
				var diffGround = Mathf.Abs(position.y - k_GroundHeight);

				var groundSnapMin = k_GroundSnapMin * distToCamera;
				var groundSnapMax = k_GroundSnapMax * distToCamera;

				var bounds = state.localBounds;
				var offset = bounds.center.y - bounds.extents.y;
				if (!pivotSnapping)
					diffGround = Mathf.Abs(position.y + offset - k_GroundHeight);

				if (diffGround < groundSnapMin)
					state.groundSnapping = true;

				if (diffGround > groundSnapMax)
					state.groundSnapping = false;

				if (state.groundSnapping)
				{
					if (pivotSnapping)
						position.y = k_GroundHeight;
					else
						position.y = k_GroundHeight - offset;
				}

				return position;
			}

			position += delta;
			return position;
		}

		public void ClearSnappingState(object caller)
		{
			m_SnappingStates.Remove(caller);
		}
	}
}
