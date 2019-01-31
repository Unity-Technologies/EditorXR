#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    public sealed class CreatePrimitiveMenuButtonUI : MonoBehaviour
    {
        [SerializeField]
        Transform m_ShapeTransform;

        [SerializeField]
        RectTransform m_BackgroundGradientTransform;

        Vector3 m_OriginalShapeLocalScale;
        Vector2 m_OriginalBackgroundSizeDelta;
        Vector2 m_HiddenBackgroundSizeDelta;

        void Awake()
        {
            m_OriginalShapeLocalScale = m_ShapeTransform.localScale;
            m_OriginalBackgroundSizeDelta = m_BackgroundGradientTransform.sizeDelta;
            m_HiddenBackgroundSizeDelta = new Vector2(0f, 0f);
            m_BackgroundGradientTransform.sizeDelta = m_HiddenBackgroundSizeDelta;

            m_ShapeTransform.localScale = Vector3.zero;

            //m_MainMenuButton.hovered += Show;
        }

        void OnDestroy()
        {
            //m_MainMenuButton.hovered -= Show;
        }

        public void Show()
        {
            m_ShapeTransform.localScale = m_OriginalShapeLocalScale;
            m_BackgroundGradientTransform.sizeDelta = m_OriginalBackgroundSizeDelta;
        }

        public void MakeVisible()
        {
        }

        public void Hide()
        {
            m_ShapeTransform.localScale = Vector3.zero;
            m_BackgroundGradientTransform.sizeDelta = m_HiddenBackgroundSizeDelta;
        }

        public void Test1()
        {
        }

        public void Test3()
        {
        }
    }
}
#endif
