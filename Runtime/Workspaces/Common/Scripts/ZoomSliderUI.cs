using System;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.Workspaces
{
    sealed class ZoomSliderUI : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Slider m_ZoomSlider;
#pragma warning restore 649

        public event Action<float> sliding;

        public Slider zoomSlider { get { return m_ZoomSlider; } }

        public void ZoomSlider(float value)
        {
            if (sliding != null)
                sliding(value);
        }
    }
}
