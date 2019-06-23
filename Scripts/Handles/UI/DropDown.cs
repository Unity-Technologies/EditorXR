using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class DropDown : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        string[] m_Options;

        [SerializeField]
        bool m_MultiSelect;

        [SerializeField]
        TextMeshProUGUI m_Label;

        [SerializeField]
        RectTransform m_OptionsPanel;

        [SerializeField]
        GridLayoutGroup m_OptionsList;

        [SerializeField]
        GameObject m_TemplatePrefab;

        [SerializeField]
        GameObject m_MultiSelectTemplatePrefab;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        Transform m_BackgroundMeshTransform;

        [SerializeField]
        int m_Value;

        [SerializeField]
        int[] m_Values = new int[0];
#pragma warning restore 649

        Coroutine m_ShowDropdownCoroutine;
        Coroutine m_HideDropdownCoroutine;
        float m_HiddenDropdownItemYSpacing;
        float m_VisibleDropdownItemYSpacing;
        float m_VisibleBackgroundMeshLocalYScale;
        float m_PreviousXRotation;
        Vector3 m_OptionsPanelOriginalLocalPosition;

        Toggle[] m_Toggles;

        public event Action<int, int[]> valueChanged;

        public int[] values
        {
            get { return m_Values; }
            set
            {
                m_Values = value;
                UpdateToggles();
                UpdateLabel();
            }
        }

        public bool multiSelect
        {
            get { return m_MultiSelect; }
            set { m_MultiSelect = value; }
        }

        public int value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                UpdateLabel();
            }
        }

        public string[] options
        {
            get { return m_Options; }
            set
            {
                m_Options = value;
                SetupOptions();
            }
        }

        void Awake()
        {
            SetupOptions();

            m_HiddenDropdownItemYSpacing = -m_OptionsList.cellSize.y;
            m_VisibleDropdownItemYSpacing = m_OptionsList.spacing.y;
            m_VisibleBackgroundMeshLocalYScale = m_BackgroundMeshTransform.localScale.y;
            m_OptionsPanelOriginalLocalPosition = m_OptionsPanel.localPosition;
        }

        void OnEnable()
        {
            m_OptionsPanel.gameObject.SetActive(false);
            m_BackgroundMeshTransform.gameObject.SetActive(false);
        }

        void Update()
        {
            var currentXRotation = transform.rotation.eulerAngles.x;
            currentXRotation = Mathf.Repeat(currentXRotation - 90, 360f); // Compensate for the rotation the lerp expects
            if (Mathf.Approximately(currentXRotation, m_PreviousXRotation))
                return; // Exit if no x rotation change occurred for this frame

            m_PreviousXRotation = currentXRotation;

            const float kLerpPadding = 1.2f; // pad lerp values increasingly as it increases, reaching intended rotation sooner
            var angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 90f);

            // add lerp padding to reach and maintain the target value sooner
            var lerpAmount = (angledAmount / 90f) * kLerpPadding;

            // offset options panel rotation according to workspace rotation angle
            const float kAdditionalLerpPadding = 1.1f;
            var parallelToWorkspaceRotation = new Vector3(0f, 0f, 0f);
            var perpendicularToWorkspaceRotation = new Vector3(-90f, 0f, 0f);
            var parallelToWorkspaceLocalPosition = new Vector3(m_OptionsPanelOriginalLocalPosition.x, m_OptionsPanelOriginalLocalPosition.y + 0.015f, m_OptionsPanelOriginalLocalPosition.z - 0.0125f);
            m_OptionsPanel.localRotation = Quaternion.Euler(Vector3.Lerp(perpendicularToWorkspaceRotation, parallelToWorkspaceRotation, lerpAmount * kAdditionalLerpPadding));
            m_OptionsPanel.localPosition = Vector3.Lerp(m_OptionsPanelOriginalLocalPosition, parallelToWorkspaceLocalPosition, lerpAmount);
        }

        void SetupOptions()
        {
            if (m_Options.Length > 0)
                UpdateLabel();

            var template = m_MultiSelect ? m_MultiSelectTemplatePrefab : m_TemplatePrefab;

            if (template)
            {
                var size = template.GetComponent<RectTransform>().rect.size;
                m_OptionsPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y * m_Options.Length);

                var listTransform = m_OptionsList.transform;

                // Clear existing options
                var children = listTransform.Cast<Transform>().ToList(); // Copy list, since destroying children changes count
                foreach (var child in children)
                    UnityObjectUtils.Destroy(child.gameObject);

                m_Toggles = new Toggle[m_Options.Length];

                for (int i = 0; i < m_Options.Length; i++)
                {
                    var optionObject = (GameObject)Instantiate(template, listTransform.position, listTransform.rotation, listTransform);

                    // Zero out Z local position
                    optionObject.transform.localPosition = new Vector3(optionObject.transform.localPosition.x, optionObject.transform.localPosition.y, 0f);

                    var optionText = optionObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (optionText)
                        optionText.text = m_Options[i];

                    var toggle = optionObject.GetComponentInChildren<Toggle>();
                    if (toggle)
                        toggle.isOn = values.Contains(i);

                    m_Toggles[i] = toggle;

                    var button = optionObject.GetComponentInChildren<Button>();
                    if (button)
                    {
                        var index = i;
                        button.onClick.AddListener(() =>
                        {
                            if (toggle)
                                toggle.isOn = !toggle.isOn;
                            OnOptionClicked(index);
                        });
                    }
                }
            }
        }

        public void OpenPanel()
        {
            this.StopCoroutine(ref m_HideDropdownCoroutine);
            this.StopCoroutine(ref m_ShowDropdownCoroutine);
            m_ShowDropdownCoroutine = StartCoroutine(ShowDropDownContents());
        }

        public void ClosePanel()
        {
            this.StopCoroutine(ref m_ShowDropdownCoroutine);
            this.StopCoroutine(ref m_HideDropdownCoroutine);
            m_HideDropdownCoroutine = StartCoroutine(HideDropDownContents());
        }

        public void LabelOverride(string text)
        {
            m_Label.text = text;
        }

        void OnOptionClicked(int val)
        {
            if (m_MultiSelect)
            {
                var list = new List<int>(values);
                if (list.Contains(val))
                    list.Remove(val);
                else
                    list.Add(val);
                m_Values = list.ToArray();
            }
            else
                m_Value = val;

            UpdateLabel();

            ClosePanel();

            if (valueChanged != null)
                valueChanged(val, m_MultiSelect ? m_Values : new[] { m_Value });
        }

        void UpdateToggles()
        {
            for (int i = 0; i < m_Toggles.Length; i++)
            {
                var toggle = m_Toggles[i];
                if (toggle)
                    toggle.isOn = m_Values.Contains(i);
            }
        }

        void UpdateLabel()
        {
            if (m_MultiSelect)
            {
                var labelText = string.Empty;
                if (values.Length > 0)
                {
                    foreach (var v in values)
                        labelText += m_Options[v] + ", ";
                    m_Label.text = labelText.Substring(0, labelText.Length - 2);
                }
                else
                    m_Label.text = "Nothing";
            }
            else
            {
                if (m_Value >= 0 && m_Value < m_Options.Length)
                    m_Label.text = m_Options[m_Value];
            }
        }

        IEnumerator ShowDropDownContents()
        {
            m_OptionsPanel.gameObject.SetActive(true);
            m_BackgroundMeshTransform.gameObject.SetActive(true);

            const float kTargetDuration = 0.5f;
            var currentAlpha = m_CanvasGroup.alpha;
            var kTargetAlpha = 1f;
            var transitionAmount = 0f;
            var velocity = 0f;
            var currentDuration = 0f;
            var currentBackgroundLocalScale = m_BackgroundMeshTransform.localScale;
            var targetBackgroundLocalScale = new Vector3(m_BackgroundMeshTransform.localScale.x, m_VisibleBackgroundMeshLocalYScale, m_BackgroundMeshTransform.localScale.z);
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                transitionAmount = MathUtilsExt.SmoothDamp(transitionAmount, 1f, ref velocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_OptionsList.spacing = new Vector2(0f, Mathf.Lerp(m_HiddenDropdownItemYSpacing, m_VisibleDropdownItemYSpacing, transitionAmount));
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount * transitionAmount);
                m_BackgroundMeshTransform.localScale = Vector3.Lerp(currentBackgroundLocalScale, targetBackgroundLocalScale, transitionAmount);
                yield return null;
            }

            m_OptionsList.spacing = new Vector2(0f, m_VisibleDropdownItemYSpacing);
            m_BackgroundMeshTransform.localScale = targetBackgroundLocalScale;
            m_CanvasGroup.alpha = 1f;
            m_ShowDropdownCoroutine = null;
        }

        IEnumerator HideDropDownContents()
        {
            const float kTargetDuration = 0.25f;
            var currentAlpha = m_CanvasGroup.alpha;
            var kTargetAlpha = 0f;
            var transitionAmount = 0f;
            var currentSpacing = m_OptionsList.spacing.y;
            var velocity = 0f;
            var currentDuration = 0f;
            var currentBackgroundLocalScale = m_BackgroundMeshTransform.localScale;
            var targetBackgroundLocalScale = new Vector3(m_BackgroundMeshTransform.localScale.x, 0f, m_BackgroundMeshTransform.localScale.z);
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                transitionAmount = MathUtilsExt.SmoothDamp(transitionAmount, 1f, ref velocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_OptionsList.spacing = new Vector2(0f, Mathf.Lerp(currentSpacing, m_HiddenDropdownItemYSpacing, transitionAmount));
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount * transitionAmount);
                m_BackgroundMeshTransform.localScale = Vector3.Lerp(currentBackgroundLocalScale, targetBackgroundLocalScale, transitionAmount);
                yield return null;
            }

            m_OptionsPanel.gameObject.SetActive(false);
            m_BackgroundMeshTransform.gameObject.SetActive(false);
            m_HideDropdownCoroutine = null;
        }
    }
}
