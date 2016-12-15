using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Extensions;

namespace UnityEngine.Experimental.EditorVR.Menus
{
	public class RadialMenuUI : MonoBehaviour
	{
		const float kPhaseOffset = 90f; // Correcting the coordinates, so that 0 degrees is at the top of the radial menu
		const int kSlotCount = 16;

		[SerializeField]
		Sprite m_MissingActionIcon;

		[SerializeField]
		Image m_SlotsMask;

		[SerializeField]
		RadialMenuSlot m_RadialMenuSlotTemplate;

		[SerializeField]
		Transform m_SlotContainer;

		List<RadialMenuSlot> m_RadialMenuSlots;
		Coroutine m_VisibilityCoroutine;

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				if (m_AlternateMenuOrigin == value)
					return;

				m_AlternateMenuOrigin = value;
				transform.SetParent(m_AlternateMenuOrigin);
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
			}
		}
		Transform m_AlternateMenuOrigin;

		public bool visible
		{
			get { return m_Visible; }
			set
			{
				if (m_Visible == value)
					return;

				m_Visible = value;

				this.StopCoroutine(ref m_VisibilityCoroutine);

				gameObject.SetActive(true);
				if (value && actions.Count > 0)
					m_VisibilityCoroutine = StartCoroutine(AnimateShow());
				else if (!value && m_RadialMenuSlots != null) // only perform hiding if slots have been initialized
					m_VisibilityCoroutine = StartCoroutine(AnimateHide());
				else if (!value)
					gameObject.SetActive(false);
			}
		}
		bool m_Visible;

		public List<ActionMenuData> actions
		{
			get { return m_Actions; }
			set
			{
				if (value != null)
				{
					m_Actions = value
						.Where(a => a.sectionName != null && a.sectionName == ActionMenuItemAttribute.kDefaultActionSectionName)
						.OrderBy(a => a.priority)
						.ToList();

					if (visible && actions.Count > 0)
					{
						this.StopCoroutine(ref m_VisibilityCoroutine);
						m_VisibilityCoroutine = StartCoroutine(AnimateShow());
					}
				}
				else if (visible && m_RadialMenuSlots != null) // only perform hiding if slots have been initialized
					visible = false;
			}
		}
		List<ActionMenuData> m_Actions;

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
						visible = false;
					}
				}
			}
		}
		bool m_PressedDown;

		public Vector2 buttonInputDirection
		{
			set
			{
				if (Mathf.Approximately(value.magnitude, 0) && !Mathf.Approximately(m_ButtonInputDirection.magnitude, 0))
				{
					foreach (var slot in m_RadialMenuSlots)
						slot.highlighted = false;
				}
				else if (value.magnitude > 0)
				{
					var angle = Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg;
					angle -= kPhaseOffset;

					// Handle lower quadrant to put it into full 360 degree range
					if (angle < 0f)
						angle += 360f;

					const float kSlotAngleRange = 360f / kSlotCount;
					const float kPadding = 0.25f;

					var index = angle / kSlotAngleRange;
					var t = index % 1f;
					// Use padding to prevent unintended button switches
					if (t >= kPadding && t <= 1f - kPadding)
					{
						m_HighlightedButton = m_RadialMenuSlots[(int)index];
						foreach (var slot in m_RadialMenuSlots)
							slot.highlighted = slot == m_HighlightedButton;
					}
				}
				m_ButtonInputDirection = value;
			}
		}
		Vector2 m_ButtonInputDirection;

		RadialMenuSlot m_HighlightedButton;

		void Start()
		{
			m_SlotsMask.gameObject.SetActive(false);
		}

		void Update()
		{
			if (m_Actions != null)
			{
				// Action icons can update after being displayed
				for (int i = 0; i < m_Actions.Count; ++i)
				{
					var action = m_Actions[i].action;
					var radialMenuSlot = m_RadialMenuSlots[i];
					if (radialMenuSlot.icon != action.icon)
						radialMenuSlot.icon = action.icon;
				}
			}
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

		void SetupRadialSlotPositions()
		{
			const float kRotationSpacing = 360f / kSlotCount;
			for (int i = 0; i < kSlotCount; ++i)
			{
				var slot = m_RadialMenuSlots[i];
				// We move in counter-clockwise direction
				slot.visibleLocalRotation = Quaternion.AngleAxis(kPhaseOffset + kRotationSpacing * i, Vector3.down);
				slot.Hide();
			}

			this.StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateHide());
		}

		void UpdateRadialSlots()
		{
			var gradientPair = UnityBrandColorScheme.sessionGradient;

			for (int i = 0; i < m_Actions.Count; ++i)
			{
				// prevent more actions being added beyond the max slot count
				if (i >= kSlotCount)
					break;

				var action = m_Actions[i].action;
				var slot = m_RadialMenuSlots[i];
				slot.gradientPair = gradientPair;
				slot.icon = action.icon ?? m_MissingActionIcon;

				var index = i; // Closure
				slot.button.onClick.RemoveAllListeners();
				slot.button.onClick.AddListener(() =>
				{
					var selectedSlot = m_RadialMenuSlots[index];
					var buttonAction = m_Actions[index].action;
					buttonAction.ExecuteAction();
					selectedSlot.icon = buttonAction.icon ?? m_MissingActionIcon;
				});
			}
		}

		IEnumerator AnimateShow()
		{
			m_SlotsMask.gameObject.SetActive(true);

			UpdateRadialSlots();

			m_SlotsMask.fillAmount = 1f;

			var revealAmount = 0f;
			var hiddenSlotRotation = RadialMenuSlot.hiddenLocalRotation;;

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

			m_VisibilityCoroutine = null;
		}

		IEnumerator AnimateHide()
		{
			if (!m_SlotsMask.gameObject.activeInHierarchy)
				yield break;

			m_SlotsMask.fillAmount = 1f;

			var revealAmount = 0f;
			var hiddenSlotRotation = RadialMenuSlot.hiddenLocalRotation;

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
			gameObject.SetActive(false);
			m_VisibilityCoroutine = null;
		}

		public void SelectionOccurred()
		{
			if (m_HighlightedButton != null)
				m_HighlightedButton.button.onClick.Invoke();
		}
	}
}
