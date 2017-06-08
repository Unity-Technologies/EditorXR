#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuButton : MonoBehaviour, ITooltip, IControlHaptics, IRayEnterHandler, IPointerClickHandler
	{
		public Button button { get { return m_Button; } }
		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Text m_ButtonDescription;
		[SerializeField]
		private Text m_ButtonTitle;

		Color m_OriginalColor;
		Transform m_RayOrigin;

		public string tooltipText { get; set; }

		public void OnPointerClick(PointerEventData eventData)
		{
			this.Pulse(m_RayOrigin, 0.5f, 0.095f, true, true);
		}

		public bool selected
		{
			set
			{
				if (value)
				{
					m_Button.transition = Selectable.Transition.None;
					m_Button.targetGraphic.color = m_Button.colors.highlightedColor;
				}
				else
				{
					m_Button.transition = Selectable.Transition.ColorTint;
					m_Button.targetGraphic.color = m_OriginalColor;
				}
			}
		}

		private void Awake()
		{
			m_OriginalColor = m_Button.targetGraphic.color;
		}

		void OnDisable()
		{
			m_RayOrigin = null;
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}

		public void OnRayEnter(RayEventData eventData)
		{
			m_RayOrigin = eventData.rayOrigin;
			this.Pulse(eventData.rayOrigin, 0.005f, 0.175f);
		}

		public void OnRayExit(RayEventData eventData)
		{
			m_RayOrigin = null;
		}
	}
}
#endif
