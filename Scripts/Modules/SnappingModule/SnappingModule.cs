using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SnappingModule : MonoBehaviour
	{
		const float k_GroundHeight = 0f;
		const float k_GroundSnapMin = 0.3f;
		const float k_GroundSnapMax = 0.5f;

		public bool groundSnapping { get; set; }

		readonly Dictionary<object, SnappingState> m_SnappingStates = new Dictionary<object, SnappingState>();

		class SnappingState
		{
			public bool groundSnapping;
			public Vector3 currentPosition;
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

				var position = state.currentPosition;
				var diffGround = Mathf.Abs(position.y - k_GroundHeight);
				if (diffGround < k_GroundSnapMin)
					state.groundSnapping = true;

				if (diffGround > k_GroundSnapMax)
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
