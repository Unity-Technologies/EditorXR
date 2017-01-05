using System;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Workspaces
{
	public class ZoomSliderUI : MonoBehaviour
	{
		public Slider zoomSlider { get { return m_ZoomSlider; } }
		[SerializeField]
		Slider m_ZoomSlider;

		public event Action<float> sliding;

		public void ZoomSlider(float value)
		{
			if (sliding != null)
				sliding(value);
		}
	}
}