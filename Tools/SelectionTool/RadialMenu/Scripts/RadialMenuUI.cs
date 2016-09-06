using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Menus
{
	public class RadialMenuUI : MonoBehaviour, IInstantiateUI
	{
		[SerializeField]
		private Image m_SlotsMask;

		[SerializeField]
		private RadialMenuSlot m_RadialMenuSlotTemplate;

		[SerializeField]
		private Transform m_SlotContainer;

		private const int m_SlotCount = 16;
		private Vector3 m_AlternateMenuOriginOriginalLocalScale;

		private List<RadialMenuSlot> m_RadialMenuSlots;
		private Coroutine m_ShowCoroutine;
		private Coroutine m_HideCoroutine;

		public Func<IAction, bool> performAction { private get; set; }
		public Func<GameObject, GameObject> instantiateUI { private get; set; }

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				m_AlternateMenuOrigin = value;
				transform.SetParent(m_AlternateMenuOrigin);
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
				m_AlternateMenuOriginOriginalLocalScale = m_AlternateMenuOrigin.localScale;
			}
		}
		private Transform m_AlternateMenuOrigin;

		public List<IAction> m_Actions;
		public List<IAction> actions
		{
			get { return m_Actions; }
			set
			{
				if (value == m_Actions) // only change visual state if the actions have changed.  Reference checking for now.
					return;

				m_Actions = value;

				if (m_ShowCoroutine != null)
				{
					StopCoroutine(m_ShowCoroutine);
					m_ShowCoroutine = null;
				}

				if (m_HideCoroutine != null)
				{
					StopCoroutine(m_HideCoroutine);
					m_HideCoroutine = null;
				}
				//TODO validate that actions & count are the same
				if (value != null && value.Count > 0)
					m_ShowCoroutine = StartCoroutine(AnimateShow());
				else
					m_HideCoroutine = StartCoroutine(AnimateHide());
			}
		}

		public bool visible
		{
			get; set;
			/*
			get { return m_VisibilityState == VisibilityState.Visible; }
			set
			{
				switch (m_VisibilityState)
				{
					case VisibilityState.TransitioningOut:
					case VisibilityState.Hidden:
						if (value)
						{
							if (m_VisibilityCoroutine != null)
								StopCoroutine(m_VisibilityCoroutine);
							m_VisibilityCoroutine = StartCoroutine(AnimateShow());
						}
						return;
					case VisibilityState.TransitioningIn:
					case VisibilityState.Visible:
						if (!value)
						{
							if (m_VisibilityCoroutine != null)
								StopCoroutine(m_VisibilityCoroutine);
							m_VisibilityCoroutine = StartCoroutine(AnimateHide());
						}
						return;
				}
			}
			*/
		}

		public void Setup()
		{
			m_RadialMenuSlots = new List<RadialMenuSlot>();

			for (int i = 0; i < m_SlotCount; ++i)
			{
				Transform menuSlot = null;
#if UNITY_EDITOR
				menuSlot = instantiateUI(m_RadialMenuSlotTemplate.gameObject).transform;
#else
				menuSlot = GameObject.Instantiate(m_RadialMenuSlotTemplate.gameObject).transform;
#endif
				var slotController = menuSlot.GetComponent<RadialMenuSlot>();
				m_RadialMenuSlots.Add(slotController);
				menuSlot.SetParent(m_SlotContainer);
				menuSlot.localPosition = Vector3.zero;
				menuSlot.localRotation = Quaternion.identity;
				menuSlot.localScale = Vector3.one;
			}
			SetupRadialSlotPositions();
		}

		private void Start()
		{
			//SetupRadialSlotPositions();
			Debug.LogError("<color=green>Beginning Radial menu Start</color>");
			//StartCoroutine(AnimateSlotReveal(8));
		}

		private void SetupRadialSlotPositions()
		{
			const float rotationSpacing = 22.5f;
			for (int i = 0; i < m_SlotCount; ++i)
			{
				m_RadialMenuSlots[i].visibleLocalRotation = Quaternion.AngleAxis(rotationSpacing * i, Vector3.up);
				// todo setup automatic hiding in start on the slot controllers
			}
		}

		private void Show(List<IAction> menuActions)
		{
			// if list count is zero, hide
			// if greather, and the icons are not the same, then hide then show with new icons
			// if the same, dont hide, just stay showing
			Debug.LogError("Show called in RadialMenuVisuals");

			// BLOCK INPUT WHILE SHOWING OR HIDING!!!!!

			/*
			b.button.onClick.RemoveAllListeners();
			b.button.onClick.AddListener(() =>
			{
				if (visible && b.node.HasValue)
					selectTool(b.node.Value, toolType);
			});
			*/

			if (menuActions != null && menuActions.Count > 0)
			{
				for (int i = 0; i < menuActions.Count; ++i)
				{
					m_RadialMenuSlots[i].iconSprite = menuActions[i].icon;
					m_RadialMenuSlots[i].button.onClick.RemoveAllListeners();
					m_RadialMenuSlots[i].button.onClick.AddListener(() =>
					{
						performAction(menuActions[i]);
					});
				}

				StartCoroutine(AnimateShow());
			}
			else
				StartCoroutine(AnimateHide());
		}

		private void Hide()
		{
			Debug.LogError("Hide called in RadialMenuVisuals");
		}

		private IEnumerator AnimateShow()
		{
			for (int i = 0; i < m_Actions.Count; ++i)
			{
				m_RadialMenuSlots[i].iconSprite = m_Actions[i].icon;
				m_RadialMenuSlots[i].button.onClick.RemoveAllListeners();
				m_RadialMenuSlots[i].button.onClick.AddListener(() =>
				{
					performAction(m_Actions[i]);
				});
			}
			
			//int slotsToReveal = m_Actions.Count;
			m_SlotsMask.fillAmount = 1f;
			//yield return new WaitForSeconds(1f);

			float revealAmount = 0f;
			Quaternion hiddenSlotRotation = RadialMenuSlot.hiddenLocalRotation;

			for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				m_RadialMenuSlots[i].enabled = true;

			while (revealAmount < 1)
			{
				revealAmount += Time.unscaledDeltaTime * 4;

				for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				{
					m_RadialMenuSlots[i].enabled = true;
					m_RadialMenuSlots[i].transform.localRotation = Quaternion.Lerp(hiddenSlotRotation,
					m_RadialMenuSlots[i].visibleLocalRotation, revealAmount);
				}

				yield return null;
			}

			revealAmount = 0;
			while (revealAmount < 1)
			{
				revealAmount += Time.unscaledDeltaTime * 0.5f;
				m_SlotsMask.fillAmount = Mathf.Lerp(m_SlotsMask.fillAmount, 0f, revealAmount);
				yield return null;
			}

			m_ShowCoroutine = null;
		}

		private IEnumerator AnimateHide()
		{
			m_SlotsMask.fillAmount = 1f;
			//yield return new WaitForSeconds(1f);

			float revealAmount = 0f;
			Quaternion hiddenSlotRotation = RadialMenuSlot.hiddenLocalRotation;

			for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				m_RadialMenuSlots[i].enabled = true;

			
			for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				m_RadialMenuSlots[i].enabled = false;

			revealAmount = 1;
			while (revealAmount > 0)
			{
				revealAmount -= Time.unscaledDeltaTime * 3;

				//m_SlotsMask.fillAmount = Mathf.Lerp(1f, m_SlotsMask.fillAmount, revealAmount * revealAmount);

				for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
					m_RadialMenuSlots[i].transform.localRotation = Quaternion.Lerp(hiddenSlotRotation, m_RadialMenuSlots[i].visibleLocalRotation, revealAmount);

				yield return null;
			}

			m_HideCoroutine = null;
		}

		private IEnumerator AnimateSlotRevealLoop(int slotsToReveal)
		{
			m_SlotsMask.fillAmount = 1f;
			//yield return new WaitForSeconds(1f);

			float revealAmount = 0f;
			Quaternion hiddenSlotRotation = RadialMenuSlot.hiddenLocalRotation;

			for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				m_RadialMenuSlots[i].enabled = true;

			while (revealAmount < 1)
			{
				revealAmount += Time.unscaledDeltaTime * 4;

				for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				{
					m_RadialMenuSlots[i].enabled = true;
					m_RadialMenuSlots[i].transform.localRotation = Quaternion.Lerp(hiddenSlotRotation,
					m_RadialMenuSlots[i].visibleLocalRotation, revealAmount);
				}

				yield return null;
			}

			revealAmount = 0;
			while (revealAmount < 1)
			{
				revealAmount += Time.unscaledDeltaTime * 0.5f;
				m_SlotsMask.fillAmount = Mathf.Lerp(m_SlotsMask.fillAmount, 0f, revealAmount);
				yield return null;
			}

			//yield return new WaitForSeconds(0.5f);

			//revealAmount = 1;
			//while (revealAmount > 0)
			//{
			//	revealAmount -= Time.unscaledDeltaTime * 0.5f;
			//	m_SlotsMask.fillAmount = Mathf.Lerp(m_SlotsMask.fillAmount, 0f, revealAmount);
			//	yield return null;
			//}

			for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				m_RadialMenuSlots[i].enabled = false;

			revealAmount = 1;
			while (revealAmount > 0)
			{
				revealAmount -= Time.unscaledDeltaTime * 3;

				//m_SlotsMask.fillAmount = Mathf.Lerp(1f, m_SlotsMask.fillAmount, revealAmount * revealAmount);

				for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
					m_RadialMenuSlots[i].transform.localRotation = Quaternion.Lerp(hiddenSlotRotation, m_RadialMenuSlots[i].visibleLocalRotation, revealAmount);

				yield return null;
			}

			//StartCoroutine(AnimateSlotReveal(8));
		}

		/*
		public void SetupButton(Sprite icon, Action<MainMenuButton> buttonCreationCallback)
		{
			var button = U.Object.Instantiate(m_ButtonTemplatePrefab.gameObject);
			button.name = buttonData.name;
			MainMenuButton mainMenuButton = button.GetComponent<MainMenuButton>();
			buttonCreationCallback(mainMenuButton);

			if (buttonData.sectionName != null)
			{
				mainMenuButton.SetData(buttonData.name, buttonData.description);

				var found = m_FaceButtons.Any(x => x.Key == buttonData.sectionName);
				if (found)
				{
					var kvp = m_FaceButtons.First(x => x.Key == buttonData.sectionName);
					kvp.Value.Add(button.transform);
				}
				else
				{
					m_FaceButtons.Add(buttonData.sectionName, new List<Transform>() { button.transform });
				}
			}
			else
				mainMenuButton.SetData(buttonData.name, string.Empty);
		}
		*/
	}
}