using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Extensions;
using GradientPair = UnityEngine.VR.Utilities.UnityBrandColorScheme.GradientPair;

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

		private const int kSlotCount = 16;

		private List<RadialMenuSlot> m_RadialMenuSlots;
		private Coroutine m_ShowCoroutine;
		private Coroutine m_HideCoroutine;
		private Coroutine m_SlotsRevealCoroutine;

		public Func<GameObject, GameObject> instantiateUI { private get; set; }

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				if (m_AlternateMenuOrigin != value)
				{
					m_AlternateMenuOrigin = value;
					transform.SetParent(m_AlternateMenuOrigin);
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
				}
			}
		}
		private Transform m_AlternateMenuOrigin;

		public bool visible
		{
			get { return m_Visible; }
			set
			{
				if (m_Visible != value)
				{
					m_Visible = value;

					StopCoroutine(ref m_ShowCoroutine);
					StopCoroutine(ref m_HideCoroutine);

					if (value && actions.Count > 0)
						m_ShowCoroutine = StartCoroutine(AnimateShow());
					else if (m_RadialMenuSlots != null) // only perform hiding if slots have been initialized
						m_HideCoroutine = StartCoroutine(AnimateHide());
				}
			}
		}
		private bool m_Visible;

		public List<ActionMenuData> actions
		{
			get { return m_Actions; }
			set
			{
				if (value != null)
				{
					m_Actions = value
						.Where(a => a.sectionName != null && a.sectionName == ActionMenuItemAttribute.kDefaultActionSectionName)
						.OrderByDescending(a => a.indexPosition)
						.ToList();

					if (visible && actions.Count > 0)
					{
						StopCoroutine(ref m_HideCoroutine);
						StopCoroutine(ref m_ShowCoroutine);
						m_ShowCoroutine = StartCoroutine(AnimateShow());
					}
				}
				else if (visible && m_RadialMenuSlots != null) // only perform hiding if slots have been initialized
					visible = false;
			}
		}
		private List<ActionMenuData> m_Actions;

		public bool pressedDown
		{
			get { return m_PressedDown; }
			set
			{
				if (m_PressedDown != value)
				{
					m_PressedDown = value;

					foreach (var slot in m_RadialMenuSlots)
					{
						if (slot == m_HighlightedButton)
							slot.pressed = true; // If the button is pressed AND this slot is the one being highlighted, set the pressed event to true
						else
							slot.pressed = false;
					}

					if (m_HighlightedButton == null)
					{
						// No button was selected on the Radial Menu. Close the radial menu, and deselect.
						Selection.activeGameObject = null;
						actions = null;
					}
				}
			}
		}
		private bool m_PressedDown;

		[SerializeField]
		private float m_InputPhaseOffset = 75f;

		private RadialMenuSlot m_HighlightedButton;
		private Vector2 m_InputMatrix;
		private float m_PreviousInputMagnitude;
		private float m_InputDirection;
		public Vector2 buttonInputDirection
		{
			set
			{
				if (Mathf.Approximately(value.magnitude, 0) && !Mathf.Approximately(m_InputDirection, 0))
				{
					m_InputDirection = 0;
					foreach (var buttonMinMaxRange in buttonRotationRange)
						buttonMinMaxRange.Key.highlighted = false;
				}
				else if (value.magnitude > 0)
				{
					m_InputMatrix = value;
					m_InputDirection = Mathf.Atan2(m_InputMatrix.y, m_InputMatrix.x) * Mathf.Rad2Deg;
					m_InputDirection += m_InputPhaseOffset;

					var angleCorrected = m_InputDirection * Mathf.Deg2Rad;
					m_InputMatrix = new Vector2(Mathf.Cos(angleCorrected), -Mathf.Sin(angleCorrected));
					m_InputDirection = Mathf.Atan2(m_InputMatrix.y, m_InputMatrix.x) * Mathf.Rad2Deg;

					foreach (var buttonMinMaxRange in buttonRotationRange)
					{
						if (actions != null && m_InputDirection > buttonMinMaxRange.Value.x && m_InputDirection < buttonMinMaxRange.Value.y)
						{
							m_HighlightedButton = buttonMinMaxRange.Key;
							m_HighlightedButton.highlighted = true;
						}
						else
							buttonMinMaxRange.Key.highlighted = false;
					}
				}
			}
		}

		private readonly Dictionary<RadialMenuSlot, Vector2> buttonRotationRange = new Dictionary<RadialMenuSlot, Vector2>();

		private void Start()
		{
			m_SlotsMask.gameObject.SetActive(false);
		}

		public void Setup()
		{
			m_RadialMenuSlots = new List<RadialMenuSlot>();
			Material slotBorderMaterial = null;

			for (int i = 0; i < kSlotCount; ++i)
			{
				Transform menuSlot = null;
				menuSlot = U.Object.Instantiate(m_RadialMenuSlotTemplate.gameObject).transform;
				menuSlot.SetParent(m_SlotContainer);
				menuSlot.localPosition = Vector3.zero;
				menuSlot.localRotation = Quaternion.identity;
				menuSlot.localScale = Vector3.one;

				var slotController = menuSlot.GetComponent<RadialMenuSlot>();
				slotController.orderIndex = i;
				m_RadialMenuSlots.Add(slotController);

				if (slotBorderMaterial == null)
					slotBorderMaterial = slotController.borderRendererMaterial;

				// Set a new shared material for the slots in a RadialMenu.
				// This isolates shader changes in a RadialMenu's border material to only the slots in a given RadialMenu
				slotController.borderRendererMaterial = slotBorderMaterial;
			}
			SetupRadialSlotPositions();
		}

		private void SetupRadialSlotPositions()
		{
			const float rotationSpacing = 22.5f;
			for (int i = 0; i < kSlotCount; ++i)
			{
				var slot = m_RadialMenuSlots[i];
				slot.visibleLocalRotation = Quaternion.AngleAxis(rotationSpacing * i, Vector3.up);

				int direction = i > 7 ? -1 : 1;
				buttonRotationRange.Add(slot, new Vector2(direction * Mathf.PingPong(rotationSpacing * i, 180f), direction * Mathf.PingPong(rotationSpacing * i + rotationSpacing, 180f)));

				Vector2 range = Vector2.zero;
				buttonRotationRange.TryGetValue(m_RadialMenuSlots[i], out range);

				slot.Hide();
			}

			if (m_HideCoroutine != null)
				StopCoroutine(m_HideCoroutine);

			m_HideCoroutine = StartCoroutine(AnimateHide());
		}

		private IEnumerator AnimateShow()
		{
			m_SlotsMask.gameObject.SetActive(true);

			GradientPair gradientPair = UnityBrandColorScheme.GetRandomGradient();
			for (int i = 0; i < m_Actions.Count && i < kSlotCount; ++i) // prevent more actions being added beyond the max slot count
			{
				var action = m_Actions[i].action;
				var slot = m_RadialMenuSlots[i];
				slot.gradientPair = gradientPair;
				slot.iconSprite = m_Actions[i].icon;

				slot.button.onClick.RemoveAllListeners();
				slot.button.onClick.AddListener(() =>
				{
					action.ExecuteAction();
				});
			}

			m_SlotsMask.fillAmount = 1f;

			float revealAmount = 0f;
			Quaternion hiddenSlotRotation = RadialMenuSlot.hiddenLocalRotation;;

			while (revealAmount < 1)
			{
				revealAmount += Time.unscaledDeltaTime * 5;

				for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				{
					if (i < m_Actions.Count)
					{
						m_RadialMenuSlots[i].Show();
						m_RadialMenuSlots[i].transform.localRotation = Quaternion.Lerp(hiddenSlotRotation, m_RadialMenuSlots[i].visibleLocalRotation, revealAmount * revealAmount);
					}
					else
						m_RadialMenuSlots[i].Hide();
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
			if (!m_SlotsMask.gameObject.activeInHierarchy)
				yield break;

			m_SlotsMask.fillAmount = 1f;

			float revealAmount = 0f;
			Quaternion hiddenSlotRotation = RadialMenuSlot.hiddenLocalRotation;

			for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
				m_RadialMenuSlots[i].Hide();

			revealAmount = 1;
			while (revealAmount > 0)
			{
				revealAmount -= Time.unscaledDeltaTime * 5;

				for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
					m_RadialMenuSlots[i].transform.localRotation = Quaternion.Lerp(hiddenSlotRotation, m_RadialMenuSlots[i].visibleLocalRotation, revealAmount);

				yield return null;
			}

			m_SlotsMask.gameObject.SetActive(false);
			m_HideCoroutine = null;
		}

		public void SelectionOccurred()
		{
			if (m_HighlightedButton != null)
			{
				for (int i = 0; i < kSlotCount; ++i)
				{
					if (m_HighlightedButton == m_RadialMenuSlots[i])
						m_Actions[i].action.ExecuteAction();
				}
			}
		}
	}
}