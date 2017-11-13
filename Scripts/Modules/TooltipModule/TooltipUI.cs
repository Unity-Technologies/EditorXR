#if UNITY_EDITOR
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class TooltipUI : MonoBehaviour, IWillRender
    {
        const float k_IconTextMinSpacing = 4;
        const float k_IconTextSpacing = 14;

        [SerializeField]
        RawImage m_DottedLine;

        [SerializeField]
        Transform[] m_Spheres;

        [SerializeField]
        Image m_Background;

        [SerializeField]
        Image m_Icon;

        [SerializeField]
        TMP_Text m_TextLeft;

        [SerializeField]
        TMP_Text m_TextRight;

        [SerializeField]
        CanvasGroup m_LeftTextCanvasGroup;

        [SerializeField]
        CanvasGroup m_RightTextCanvasGroup;

        [SerializeField]
        LayoutElement m_LeftSpacer;

        [SerializeField]
        LayoutElement m_RightSpacer;

        [SerializeField]
        SkinnedMeshRenderer m_BackgroundRenderer;

        [SerializeField]
        SkinnedMeshRenderer m_BackgroundOutlineRenderer;

        int m_OriginalRightPaddingAmount;
        int m_OriginalTopPaddingAmount;
        int m_OriginalBottomPaddingAmount;
        int m_OriginalLeftPaddingAmount;

        TextAlignment m_Alignment;

        Coroutine m_AnimateShowLeftSideTextCoroutine;
        Coroutine m_AnimateShowRightSideTextCoroutine;

        public RawImage dottedLine { get { return m_DottedLine; } }
        public Transform[] spheres { get { return m_Spheres; } }
        public Image background { get { return m_Background; } }
        public RectTransform rectTransform { get { return m_Background.rectTransform; } }

        public event Action becameVisible;

        public void Show(string text, TextAlignment alignment, Sprite iconSprite = null)
        {
            // if Icon null, fade out opacity of current icon
            // if icon is not null, fade out current, fade in new icon
            var validText = !string.IsNullOrEmpty(text);
            var iconVisible = iconSprite != null;
            m_Icon.sprite = iconSprite;
            m_Icon.enabled = iconVisible;
            switch (alignment)
            {
                case TextAlignment.Center:
                case TextAlignment.Left:
                    // Treat center as left justified, aside from horizontal offset placement
                    m_TextRight.text = text;
                    m_TextRight.gameObject.SetActive(validText);
                    m_TextLeft.gameObject.SetActive(false);
                    m_RightSpacer.minWidth = validText ? iconVisible ? k_IconTextSpacing : 0 : 0;
                    m_LeftSpacer.minWidth = validText ? iconVisible ? k_IconTextMinSpacing : 8 : 0; ;
                    break;
                case TextAlignment.Right:
                    m_TextLeft.text = text;
                    m_TextRight.gameObject.SetActive(false);
                    m_TextLeft.gameObject.SetActive(validText);
                    m_RightSpacer.minWidth = validText ? iconVisible ? k_IconTextMinSpacing : 8 : 0;
                    m_LeftSpacer.minWidth = validText ? iconVisible ? k_IconTextSpacing : 0 : 0;
                    break;
            }

            if (!validText && iconVisible)
            {
                // Set rounded corners
                m_BackgroundRenderer.SetBlendShapeWeight(0, 0);
                m_BackgroundOutlineRenderer.SetBlendShapeWeight(0, 0);
            }
            else
            {
                // Set sharper/square corners
                m_BackgroundRenderer.SetBlendShapeWeight(0, 90);
                m_BackgroundOutlineRenderer.SetBlendShapeWeight(0, 90);
            }
        }

        public void OnBecameVisible()
        {
            if (becameVisible != null)
                becameVisible();
        }

        public void OnBecameInvisible()
        {
        }
    }
}
#endif
