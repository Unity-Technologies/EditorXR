using System;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Menus
{
	public class MainMenuButton : MonoBehaviour, IRayEnterHandler, IRayExitHandler
	{
		public Button button { get { return m_Button; } }
		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Text m_ButtonDescription;
		[SerializeField]
		private Text m_ButtonTitle;

		public Action clicked;

		/// <summary>
		/// The node of the ray that hovering over the button
		/// </summary>
		public Node? node { get; private set; }

		private void Awake()
		{
			m_Button.onClick.AddListener(OnButtonClicked);
		}

		private void OnDestroy()
		{
			m_Button.onClick.RemoveListener(OnButtonClicked);
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}

		private void OnButtonClicked()
		{
			if (clicked != null)
				clicked();
		}

		public void OnRayEnter(RayEventData eventData)
		{
			// Track which pointer is over us, so this information can supply context (e.g. selecting a tool for a different hand)
			node = eventData.node;
		}

		public void OnRayExit(RayEventData eventData)
		{
			if (node == eventData.node)
				node = null;
		}
	}
}