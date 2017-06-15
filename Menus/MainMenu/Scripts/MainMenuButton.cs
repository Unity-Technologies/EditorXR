#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuButton : MonoBehaviour, ITooltip, IControlHaptics, IRayEnterHandler, IPointerClickHandler, IRayToNode
	{
		public Button button { get { return m_Button; } }
		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Text m_ButtonDescription;

		[SerializeField]
		private Text m_ButtonTitle;

		[SerializeField]
		HapticPulse m_ClickPulse;

		[SerializeField]
		HapticPulse m_HoverPulse;

		Color m_OriginalColor;
		Node? m_InputNode;

		public string tooltipText { get; set; }

		public Func<Transform, Node?> requestNodeFromRayOrigin { private get; set; }

		public void OnPointerClick(PointerEventData eventData)
		{
			this.Pulse(m_InputNode, m_ClickPulse);
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
			m_InputNode = null;
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}

		public void OnRayEnter(RayEventData eventData)
		{
			m_InputNode = requestNodeFromRayOrigin(eventData.rayOrigin);
			this.Pulse(m_InputNode, m_HoverPulse);
		}

		public void OnRayExit(RayEventData eventData)
		{
			m_InputNode = null;
		}
	}
}
#endif
