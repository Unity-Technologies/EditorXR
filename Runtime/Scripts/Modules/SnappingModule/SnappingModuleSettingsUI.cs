using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR
{
    class SnappingModuleSettingsUI : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Toggle m_SnappingEnabled;

        [SerializeField]
        Toggle m_GroundSnappingEnabled;

        [SerializeField]
        Toggle m_SurfaceSnappingEnabled;

        [SerializeField]
        Toggle m_PivotSnappingEnabled;

        [SerializeField]
        Toggle m_RotationSnappingEnabled;

        [SerializeField]
        Toggle m_LimitRadius;

        [SerializeField]
        Toggle m_ManipulatorSnappingEnabled;

        [SerializeField]
        Toggle m_DirectSnappingEnabled;
#pragma warning restore 649

        public Toggle snappingEnabled
        {
            get { return m_SnappingEnabled; }
        }

        public Toggle groundSnappingEnabled
        {
            get { return m_GroundSnappingEnabled; }
        }

        public Toggle surfaceSnappingEnabled
        {
            get { return m_SurfaceSnappingEnabled; }
        }

        public Toggle pivotSnappingEnabled
        {
            get { return m_PivotSnappingEnabled; }
        }

        public Toggle rotationSnappingEnabled
        {
            get { return m_RotationSnappingEnabled; }
        }

        public Toggle limitRadius
        {
            get { return m_LimitRadius; }
        }

        public Toggle manipulatorSnappingEnabled
        {
            get { return m_ManipulatorSnappingEnabled; }
        }

        public Toggle directSnappingEnabled
        {
            get { return m_DirectSnappingEnabled; }
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
