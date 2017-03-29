#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
	public class SnappingModuleUI : MonoBehaviour
	{
		[SerializeField]
		Toggle m_SnappingEnabled;

		[SerializeField]
		Toggle m_GroundSnapping;

		[SerializeField]
		Toggle m_SurfaceSnapping;

		[SerializeField]
		Toggle m_PivotSnapping;

		[SerializeField]
		Toggle m_SnapRotation;

		[SerializeField]
		Toggle m_LocalOnly;

		[SerializeField]
		Toggle m_ManipulatorSnapping;

		[SerializeField]
		Toggle m_DirectSnapping;

		public Toggle snappingEnabled
		{
			get { return m_SnappingEnabled; }
		}

		public Toggle groundSnapping
		{
			get { return m_GroundSnapping; }
		}

		public Toggle surfaceSnapping
		{
			get { return m_SurfaceSnapping; }
		}

		public Toggle pivotSnapping
		{
			get { return m_PivotSnapping; }
		}

		public Toggle snapRotation
		{
			get { return m_SnapRotation; }
		}

		public Toggle localOnly
		{
			get { return m_LocalOnly; }
		}

		public Toggle manipulatorSnapping
		{
			get { return m_ManipulatorSnapping; }
		}

		public Toggle directSnapping
		{
			get { return m_DirectSnapping; }
		}

		public void SetToggleValue(Toggle toggle, bool isOn)
		{
			var toggleGroup = toggle.GetComponentInParent<ToggleGroup>();
			var toggles = toggleGroup.GetComponentsInChildren<Toggle>();
			foreach (var t in toggles)
			{
				if (t != toggle)
				{
					t.isOn = !isOn;
				}
			}
			toggle.isOn = isOn;
		}
	}
}
#endif