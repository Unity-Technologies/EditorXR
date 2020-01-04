using TMPro;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.XR;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.Tools
{
    sealed class ViewerScaleVisuals : MonoBehaviour, IUsesViewerScale
    {
#pragma warning disable 649
        [SerializeField]
        float m_IconTranslateCoefficient = -0.16f;

        [SerializeField]
        float m_IconTranslateOffset = 0.08f;

        [SerializeField]
        XRLineRenderer m_Line;

        [SerializeField]
        Transform m_IconsContainer;

        [SerializeField]
        TextMeshProUGUI m_ScaleText;

        [SerializeField]
        Sprite[] m_Icons;

        [SerializeField]
        GameObject m_IconPrefab;
#pragma warning restore 649

        float m_LineWidth;

        public Transform leftHand { private get; set; }
        public Transform rightHand { private get; set; }

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        void Awake()
        {
            foreach (var icon in m_Icons)
            {
                var image = Instantiate(m_IconPrefab, m_IconsContainer, false).GetComponent<Image>();
                image.sprite = icon;
            }

            m_LineWidth = m_Line.widthStart;
            var onTopMaterial = m_ScaleText.materialForRendering;
            onTopMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        void OnEnable()
        {
            if (leftHand && rightHand)
                SetPosition();
        }

        void Update()
        {
            SetPosition();
        }

        void SetPosition()
        {
            var iconContainerLocal = m_IconsContainer.localPosition;
            var viewerScale = this.GetViewerScale();
            iconContainerLocal.x = Mathf.Log10(viewerScale) * m_IconTranslateCoefficient + m_IconTranslateOffset;
            m_IconsContainer.localPosition = iconContainerLocal;

            var camera = CameraUtils.GetMainCamera().transform;
            var leftToRight = leftHand.position - rightHand.position;

            // If hands reverse, switch hands
            if (Vector3.Dot(leftToRight, camera.right) > 0)
            {
                leftToRight *= -1;
                var tmp = leftHand;
                leftHand = rightHand;
                rightHand = tmp;
            }

            transform.position = rightHand.position + leftToRight * 0.5f;
            transform.rotation = Quaternion.LookRotation(leftToRight, camera.position - transform.position);

            leftToRight = transform.InverseTransformVector(leftToRight);
            var length = leftToRight.magnitude * 0.5f;
            m_Line.SetPosition(0, Vector3.left * length);
            m_Line.SetPosition(1, Vector3.right * length);
            var lineWidth = m_LineWidth * viewerScale;
            m_Line.widthStart = lineWidth;
            m_Line.widthEnd = lineWidth;
            m_ScaleText.text = string.Format("Viewer Scale: {0:f2}", viewerScale);
        }
    }
}
