using System;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.UI;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class TooltipUI : MonoBehaviour, IWillRender
    {
        const float k_IconTextMinSpacing = 4;
        const float k_IconTextSpacing = 14;

#pragma warning disable 649
        [SerializeField]
        RawImage m_DottedLine;

        [SerializeField]
        Transform[] m_Spheres;

        [SerializeField]
        RectTransform m_Background;

        [SerializeField]
        Image m_Icon;

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        TMP_Text m_TextLeft;

        [SerializeField]
        TMP_Text m_TextRight;
#endif
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
#pragma warning restore 649

        int m_OriginalRightPaddingAmount;
        int m_OriginalTopPaddingAmount;
        int m_OriginalBottomPaddingAmount;
        int m_OriginalLeftPaddingAmount;

        TextAlignment m_Alignment;

        Coroutine m_AnimateShowLeftSideTextCoroutine;
        Coroutine m_AnimateShowRightSideTextCoroutine;

        public RawImage dottedLine { get { return m_DottedLine; } }
        public Transform[] spheres { get { return m_Spheres; } }
        public RectTransform rectTransform { get { return m_Background; } }

        public event Action becameVisible;

        public Action<IWillRender> removeSelf { get; set; }

        public void Show(string text, TextAlignment alignment, Sprite iconSprite = null)
        {
            // if Icon null, fade out opacity of current icon
            // if icon is not null, fade out current, fade in new icon
            var iconVisible = iconSprite != null;
            m_Icon.sprite = iconSprite;
            m_Icon.enabled = iconVisible;
            var validText = !string.IsNullOrEmpty(text);
            switch (alignment)
            {
                case TextAlignment.Center:
                case TextAlignment.Left:
                    // Treat center as left justified, aside from horizontal offset placement
#if INCLUDE_TEXT_MESH_PRO
                    m_TextRight.text = text;
                    m_TextRight.gameObject.SetActive(validText);
                    m_TextLeft.gameObject.SetActive(false);
#endif
                    m_RightSpacer.minWidth = validText ? iconVisible ? k_IconTextSpacing : 0 : 0;
                    m_LeftSpacer.minWidth = validText ? iconVisible ? k_IconTextMinSpacing : 8 : 0;
                    break;
                case TextAlignment.Right:
#if INCLUDE_TEXT_MESH_PRO
                    m_TextLeft.text = text;
                    m_TextRight.gameObject.SetActive(false);
                    m_TextLeft.gameObject.SetActive(validText);
#endif
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
