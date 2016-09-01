using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Menus
{
	public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IRayHoverHandler
	{
		public Button button { get { return m_Button; } }
		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Text m_ButtonDescription;
		[SerializeField]
		private Text m_ButtonTitle;

		public Action<Transform> ButtonEntered;
		public Action clicked;

		/// <summary>
		/// The node of the ray that is hovering over the button
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

		public void OnPointerEnter(PointerEventData eventData)
		{
			//Debug.LogError("<color=green>OnPointerEnter called on MainMenuButton</color>");
 		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Debug.LogError("<color=green>OnPointerExit called on MainMenuButton</color>");
		}

		public void OnRayHover(RayEventData eventData)
		{
			Debug.LogError("<color=green>OnPointerEnter called on MainMenuButton</color>");
			Action<Transform> ButtonEnteredHandler = ButtonEntered;
			if (ButtonEnteredHandler != null)
				ButtonEnteredHandler(transform);
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