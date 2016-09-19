using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.VR.Workspaces
{
	public class ZoomSliderUI : MonoBehaviour
	{
		public Slider zoomSlider { get { return m_ZoomSlider; } }

		[SerializeField]
		private Slider m_ZoomSlider;

		public event Action<float> sliding = delegate { };

		public void ZoomSlider(float value)
		{
			if (sliding != null)
				sliding(value);
		}
	}
}