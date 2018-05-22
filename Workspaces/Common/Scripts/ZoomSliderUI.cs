
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class ZoomSliderUI : MonoBehaviour
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

