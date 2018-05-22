
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class FilterUI : MonoBehaviour, IUsesStencilRef
    {
        const string k_AllText = "All";

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        TextMeshProUGUI m_SummaryText;

        [SerializeField]
        TextMeshProUGUI m_DescriptionText;
#else
        [SerializeField]
        Text m_SummaryText;

        [SerializeField]
        Text m_DescriptionText;
#endif

        [SerializeField]
        RectTransform m_ButtonList;

        [SerializeField]
        GameObject m_ButtonPrefab;

        [SerializeField]
        Color m_ActiveColor;

        [SerializeField]
        Color m_DisableColor;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        GridLayoutGroup m_ButtonListGrid;

        [SerializeField]
        CanvasGroup m_ButtonListCanvasGroup;

        [SerializeField]
        MeshRenderer m_Background;

        [SerializeField]
        WorkspaceButton m_VisibilityButton;

        [SerializeField]
        WorkspaceButton m_SummaryButton;

        public string searchQuery
        {
            get { return m_SearchQuery; }
        }

        string m_SearchQuery = string.Empty;

        FilterButtonUI[] m_VisibilityButtons;
        Coroutine m_ShowUICoroutine;
        Coroutine m_HideUICoroutine;
        Coroutine m_ShowButtonListCoroutine;
        Coroutine m_HideButtonListCoroutine;
        float m_HiddenButtonListYSpacing;
        List<string> m_FilterTypes;
        Material m_BackgroundMaterial;

        public List<string> filterList
        {
            set
            {
                // Clean up old buttons
                if (m_VisibilityButtons != null)
                {
                    foreach (var button in m_VisibilityButtons)
                    {
                        ObjectUtils.Destroy(button.gameObject);
                    }
                }

                m_FilterTypes = value;

                if (addDefaultOption)
                    m_FilterTypes.Insert(0, k_AllText);

                // Generate new button list
                m_VisibilityButtons = new FilterButtonUI[m_FilterTypes.Count];
                for (int i = 0; i < m_VisibilityButtons.Length; i++)
                {
                    var button = ObjectUtils.Instantiate(m_ButtonPrefab, m_ButtonList, false).GetComponent<FilterButtonUI>();
                    m_VisibilityButtons[i] = button;

                    button.clicked += rayOrigin => { OnFilterClick(button); };
                    button.clicked += OnClicked;
                    button.hovered += OnHovered;
                    button.text.text = m_FilterTypes[i];
                }
            }
        }

        public byte stencilRef { get; set; }

#if INCLUDE_TEXT_MESH_PRO
        public TextMeshProUGUI summaryText { get { return m_SummaryText; } }
        public TextMeshProUGUI descriptionText { get { return m_DescriptionText; } }
#else
        public Text summaryText { get; set; }
        public Text descriptionText { get; set; }
#endif

        public bool addDefaultOption { private get; set; }

        public event Action<Transform> buttonHovered;
        public event Action<Transform> buttonClicked;
        public event Action filterChanged;

        void Awake()
        {
            addDefaultOption = true;
            m_HiddenButtonListYSpacing = -m_ButtonListGrid.cellSize.y;
        }

        void Start()
        {
            m_BackgroundMaterial = MaterialUtils.GetMaterialClone(m_Background);
            m_BackgroundMaterial.SetInt("_StencilRef", stencilRef);

            if (m_VisibilityButton)
            {
                m_VisibilityButton.clicked += OnVisibilityButtonClicked;
                m_VisibilityButton.hovered += OnHovered;
            }

            m_SummaryButton.clicked += OnVisibilityButtonClicked;
            m_SummaryButton.hovered += OnHovered;
        }

        void OnDestroy()
        {
            ObjectUtils.Destroy(m_BackgroundMaterial);
        }

        public void SetListVisibility(bool show)
        {
            if (show)
            {
                this.StopCoroutine(ref m_HideUICoroutine);
                m_HideUICoroutine = StartCoroutine(HideUIContent());

                this.StopCoroutine(ref m_ShowButtonListCoroutine);
                m_ShowButtonListCoroutine = StartCoroutine(ShowButtonList());
            }
            else if (m_ButtonList.gameObject.activeSelf)
            {
                this.StopCoroutine(ref m_ShowUICoroutine);
                m_ShowUICoroutine = StartCoroutine(ShowUIContent());

                this.StopCoroutine(ref m_HideButtonListCoroutine);
                m_HideButtonListCoroutine = StartCoroutine(HideButtonList());
            }
        }

        void OnFilterClick(FilterButtonUI clickedButton)
        {
            for (int i = 0; i < m_VisibilityButtons.Length; i++)
                if (clickedButton == m_VisibilityButtons[i])
                    m_SearchQuery = i == 0 && addDefaultOption ? string.Empty : m_FilterTypes[i];

            foreach (FilterButtonUI button in m_VisibilityButtons)
            {
                if (button == clickedButton)
                    button.color = m_ActiveColor;
                else
                    button.color = !string.IsNullOrEmpty(m_SearchQuery) ? m_DisableColor : m_ActiveColor;
            }

            switch (clickedButton.text.text)
            {
                case k_AllText:
                    m_SummaryText.text = clickedButton.text.text;
                    m_DescriptionText.text = "All objects are visible";
                    break;

                default:
                    m_SummaryText.text = clickedButton.text.text + "s";
                    m_DescriptionText.text = "Only " + m_SummaryText.text + " are visible";
                    break;
            }

            if (filterChanged != null)
                filterChanged();
        }

        IEnumerator ShowUIContent()
        {
            var currentAlpha = m_CanvasGroup.alpha;
            var kTargetAlpha = 1f;
            var transitionAmount = Time.deltaTime;
            while (transitionAmount < 1)
            {
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
                transitionAmount = transitionAmount + Time.deltaTime;
                yield return null;
            }

            m_CanvasGroup.alpha = kTargetAlpha;
            m_ShowUICoroutine = null;
        }

        IEnumerator HideUIContent()
        {
            var currentAlpha = m_CanvasGroup.alpha;
            var kTargetAlpha = 0f;
            var transitionAmount = Time.deltaTime;
            var kSpeedMultiplier = 3;
            while (transitionAmount < 1)
            {
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
                transitionAmount = transitionAmount + Time.deltaTime * kSpeedMultiplier;
                yield return null;
            }

            m_CanvasGroup.alpha = kTargetAlpha;
            m_HideUICoroutine = null;
        }

        IEnumerator ShowButtonList()
        {
            m_ButtonList.gameObject.SetActive(true);

            const float targetDuration = 0.5f;
            const float targetMinSpacing = 0.0015f;
            var currentAlpha = m_ButtonListCanvasGroup.alpha;
            var kTargetAlpha = 1f;
            var transitionAmount = 0f;
            var velocity = 0f;
            var currentDuration = 0f;
            while (currentDuration < targetDuration)
            {
                currentDuration += Time.deltaTime;
                transitionAmount = MathUtilsExt.SmoothDamp(transitionAmount, 1f, ref velocity, targetDuration, Mathf.Infinity, Time.deltaTime);
                m_ButtonListGrid.spacing = new Vector2(0f, Mathf.Lerp(m_HiddenButtonListYSpacing, targetMinSpacing, transitionAmount));
                m_ButtonListCanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
                yield return null;
            }

            m_ButtonListGrid.spacing = Vector2.one * targetMinSpacing;
            m_ButtonListCanvasGroup.alpha = 1f;
            m_ShowButtonListCoroutine = null;
        }

        IEnumerator HideButtonList()
        {
            const float kTargetDuration = 0.25f;
            var currentAlpha = m_ButtonListCanvasGroup.alpha;
            var kTargetAlpha = 0f;
            var transitionAmount = 0f;
            var currentSpacing = m_ButtonListGrid.spacing.y;
            var velocity = 0f;
            var currentDuration = 0f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                transitionAmount = MathUtilsExt.SmoothDamp(transitionAmount, 1f, ref velocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_ButtonListGrid.spacing = new Vector2(0f, Mathf.Lerp(currentSpacing, m_HiddenButtonListYSpacing, transitionAmount));
                m_ButtonListCanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
                yield return null;
            }

            m_ButtonList.gameObject.SetActive(false);
            m_HideButtonListCoroutine = null;
        }

        void OnVisibilityButtonClicked(Transform rayOrigin)
        {
            SetListVisibility(true);
            OnClicked(rayOrigin);
        }

        void OnClicked(Transform rayOrigin)
        {
            if (buttonClicked != null)
                buttonClicked(rayOrigin);
        }

        void OnHovered(Transform rayOrigin)
        {
            if (buttonHovered != null)
                buttonHovered(rayOrigin);
        }
    }
}

