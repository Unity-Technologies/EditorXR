using System;
using TMPro;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class FolderListItem : EditorXRNestedListViewItem<FolderData, int>
    {
        const float k_Margin = 0.01f;
        const float k_Indent = 0.02f;

        const float k_ExpandArrowRotateSpeed = 0.4f;

#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_Text;

        [SerializeField]
        BaseHandle m_Cube;

        [SerializeField]
        BaseHandle m_ExpandArrow;

        [SerializeField]
        Material m_NoClipCubeMaterial;

        [SerializeField]
        Material m_NoClipExpandArrowMaterial;

        [SerializeField]
        Color m_HoverColor;

        [SerializeField]
        Color m_SelectedColor;
#pragma warning restore 649

        Color m_NormalColor;

        bool m_Hovering;

        Renderer m_CubeRenderer;

        Transform m_CubeTransform;

        public Material cubeMaterial { get { return m_CubeRenderer.sharedMaterial; } }

        public Action<int> selectFolder { private get; set; }

        public override void Setup(FolderData listData, bool firstTime = true)
        {
            base.Setup(listData, firstTime);

            if (firstTime)
            {
                // Cube material might change for hover state, so we always instance it
                m_CubeRenderer = m_Cube.GetComponent<Renderer>();
                m_NormalColor = m_CubeRenderer.sharedMaterial.color;
                MaterialUtils.GetMaterialClone(m_CubeRenderer);

                m_ExpandArrow.dragEnded += ToggleExpanded;
                m_Cube.dragStarted += SelectFolder;
                m_Cube.dragEnded += ToggleExpanded;

                m_Cube.hoverStarted += OnHoverStarted;
                m_Cube.hoverEnded += OnHoverEnded;
            }

            m_CubeTransform = m_Cube.transform;
            m_Text.text = listData.name;

            m_ExpandArrow.gameObject.SetActive(listData.children != null);
            m_Hovering = false;
        }

        public void SetMaterials(Material textMaterial, Material expandArrowMaterial)
        {
            m_Text.fontMaterial = textMaterial;
            m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
        }

        public void UpdateSelf(float width, int depth, bool expanded, bool selected)
        {
            var cubeScale = m_CubeTransform.localScale;
            cubeScale.x = width;
            m_CubeTransform.localScale = cubeScale;

            var expandArrowTransform = m_ExpandArrow.transform;

            var arrowWidth = expandArrowTransform.localScale.x * 0.5f;
            var halfWidth = width * 0.5f;
            var indent = k_Indent * depth;
            const float doubleMargin = k_Margin * 2;
            expandArrowTransform.localPosition = new Vector3(k_Margin + indent - halfWidth, expandArrowTransform.localPosition.y, 0);

            // Text is next to arrow, with a margin and indent, rotated toward camera
            var textTransform = m_Text.transform;
            m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - doubleMargin - indent) * 1 / textTransform.localScale.x);
            textTransform.localPosition = new Vector3(doubleMargin + indent + arrowWidth - halfWidth, textTransform.localPosition.y, 0);

            textTransform.localRotation = CameraUtils.LocalRotateTowardCamera(transform.parent);

            UpdateArrow(expanded);

            // Set selected/hover/normal color
            if (selected)
                m_CubeRenderer.sharedMaterial.color = m_SelectedColor;
            else if (m_Hovering)
                m_CubeRenderer.sharedMaterial.color = m_HoverColor;
            else
                m_CubeRenderer.sharedMaterial.color = m_NormalColor;
        }

        public void UpdateArrow(bool expanded, bool immediate = false)
        {
            var expandArrowTransform = m_ExpandArrow.transform;

            // Rotate arrow for expand state
            expandArrowTransform.localRotation = Quaternion.Lerp(expandArrowTransform.localRotation,
                Quaternion.AngleAxis(90f, Vector3.right) * (expanded ? Quaternion.AngleAxis(90f, Vector3.back) : Quaternion.identity),
                immediate ? 1f : k_ExpandArrowRotateSpeed);
        }

        void ToggleExpanded(BaseHandle handle, HandleEventData eventData)
        {
            ToggleExpanded();
        }

        void SelectFolder(BaseHandle handle, HandleEventData eventData)
        {
            selectFolder(data.index);
        }

        void OnHoverStarted(BaseHandle handle, HandleEventData eventData)
        {
            m_Hovering = true;
        }

        void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
        {
            m_Hovering = false;
        }

        void OnDestroy()
        {
            if (m_CubeRenderer)
                ObjectUtils.Destroy(m_CubeRenderer.sharedMaterial);
        }
    }
}
