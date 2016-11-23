using System;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;
using UnityEngine.VR.UI;

namespace UnityEngine.VR.Menus
{
	public class ActiveToolButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectTool
	{
		readonly Vector3 m_OriginalLocalPosition = new Vector3(0f, 0f, -0.035f);

		public event Action<Transform> selected = delegate { };
		public Transform menuOrigin { private get; set; }
		public Transform activeToolRayOrigin { get; set; }
		public Func<Transform, Type, bool> selectTool { private get; set; }

		public ITool activeTool
		{
			get
			{
				return m_ActiveTool;
			}

			set
			{
				if (m_ActiveTool == value)
					return;

				m_ActiveTool = value;
				if (m_ActiveTool != null)
				{
					Debug.LogError("<color=green>Asigning tool to ActiveToolbutton : </color>" + m_ActiveTool.ToString());
					m_VRButton.SetContent(m_ActiveTool.ToString().Remove(0, 10)); // remove the initial "EditorVR (" characters, the button will only use the first character
					m_VRButton.visible = true;
				}
				else
				{
					Debug.LogError("<color=red>Asigning tool to ActiveToolbutton : </color>" + m_ActiveTool.ToString());
					m_VRButton.visible = false;
				}
			}
		}
		private ITool m_ActiveTool;

		[SerializeField]
		private VRButton m_VRButton;

		[SerializeField]
		private Transform m_ButtonBase;

		private void Start()
		{
			//Debug.LogError("<color=orange>AWAKE called in ActiveToolbutton <--------</color>");
			transform.localPosition = m_OriginalLocalPosition;
			transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
			transform.localScale = Vector3.one;
			m_VRButton.InstantHide();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (!m_VRButton.visible)
				return;

			 var rayEventData = eventData as RayEventData;
			if (rayEventData != null)
				m_VRButton.highlighted = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (!m_VRButton.visible)
				return;

			var rayEventData = eventData as RayEventData;
			if (rayEventData != null)
				m_VRButton.highlighted = false;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!m_VRButton.visible)
				return;

			if (m_ActiveTool != null && activeToolRayOrigin)
				selectTool(activeToolRayOrigin, activeTool.GetType());
			else
				Debug.LogError("<color=red>OnPointerClick called with NULL ActiveToolbutton <--------</color>");
		}
	}
}
