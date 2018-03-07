#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
    public class SpatialUIMenuElement : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_Text;

        [SerializeField]
        Image m_Icon;

        Transform m_Transform;
        Action m_SelectedAction;

        public Transform transform { get { return m_Transform; } }
        public Action selectedAction { get { return m_SelectedAction; } }

        public void Setup(Transform transform, Action selectedAction, String displayedText = null, Sprite sprite = null)
        {
            if (selectedAction == null)
            {
                Debug.LogWarning("Cannot setup SpatialUIMenuElement without an assigned action.");
                ObjectUtils.Destroy(gameObject);
                return;
            }

            m_SelectedAction = selectedAction;
            m_Transform = transform;

            if (sprite != null) // Displaying a sprite icon instead of text
            {
                m_Icon.gameObject.SetActive(true);
                m_Text.gameObject.SetActive(false);
                m_Icon.sprite = sprite;
            }
            else // Displaying text instead of a sprite icon
            {
                m_Icon.gameObject.SetActive(false);
                m_Text.gameObject.SetActive(true);
                m_Text.text = displayedText;
            }

        }

        // TODO perform animated reveal of content after setup
        public void AnimateShow()
        {
            Debug.Log("Performing AnimateShow for SpatialUIMenuElement : " + m_Text.text);
        }

        public void AnimateHide()
        {
            Debug.Log("Performing AnimateHide for SpatialUIMenuElement : " + m_Text.text);
        }
    }
}
#endif
