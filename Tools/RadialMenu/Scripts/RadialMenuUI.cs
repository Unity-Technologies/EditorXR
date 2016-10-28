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
	public class RadialMenuUI : MonoBehaviour
	{
		const int kSlotCount = 16;
		const float kSelectMagnitudeThreshold = 0.5f;
		const float kSelectDurationLimit = 0.25f;

		[SerializeField]
		Sprite m_MissingActionIcon;

		[SerializeField]
		Image m_SlotsMask;

		[SerializeField]
		RadialMenuSlot m_RadialMenuSlotTemplate;

		[SerializeField]
		Transform m_SlotContainer;

		List<RadialMenuSlot> m_RadialMenuSlots;
		Coroutine m_ShowCoroutine;
		Coroutine m_HideCoroutine;
		Vector2 m_DragStartVector;
		float m_DragMagnitude;
		float m_DragSelectMaxTime;

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

				this.StopCoroutine(ref m_ShowCoroutine);
				this.StopCoroutine(ref m_HideCoroutine);

				gameObject.SetActive(true);
				if (value && actions.Count > 0)
					m_ShowCoroutine = StartCoroutine(AnimateShow());
				else if (!value && m_RadialMenuSlots != null) // only perform hiding if slots have been initialized
					m_HideCoroutine = StartCoroutine(AnimateHide());
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
						.OrderByDescending(a => a.priority)
						.ToList();

					if (visible && actions.Count > 0)
					{
						this.StopCoroutine(ref m_HideCoroutine);
						this.StopCoroutine(ref m_ShowCoroutine);
						m_ShowCoroutine = StartCoroutine(AnimateShow());
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
						actions = null;
					}
				}
			}
		}
		bool m_PressedDown;

		[SerializeField]
		float m_InputPhaseOffset = 75f;

		RadialMenuSlot m_HighlightedButton;
		Vector2 m_InputMatrix;
		float m_InputDirection;

		readonly Dictionary<RadialMenuSlot, Vector2> buttonRotationRange = new Dictionary<RadialMenuSlot, Vector2>();

		public Vector2 buttonInputDirection
		{
			set
			{
				if (Mathf.Approximately(value.magnitude, 0) && !Mathf.Approximately(m_InputDirection, 0))
				{
					if (m_HighlightedButton && Time.realtimeSinceStartup < m_DragSelectMaxTime && m_DragMagnitude > 0 && m_DragMagnitude < kSelectMagnitudeThreshold) // check if a drag within the selection threshold occurred
					{
						m_HighlightedButton.button.onClick.Invoke();
						selectItem(); // call the externally set select action
					}

					m_DragMagnitude = 0f;
					m_DragStartVector = new Vector2();
					m_InputDirection = 0f;
					m_DragSelectMaxTime = 0f;
				}
				else if (value.magnitude > 0)
				{
					if (m_DragStartVector.magnitude == 0f)
					{
						m_DragStartVector = value; // set new starting drag vector position, if the drag start vector was previously reset
						m_DragSelectMaxTime = Time.realtimeSinceStartup + kSelectDurationLimit;
					}
					else
					{
						// set the magnitude of starting-to-current drag positions
						// this is used to detect the drag threshold for a tap(select) or a drag(highlight)
						// a drag with small magnitude triggers a selection of the currently highlighted menu-item (if there exists a highlighted item)
						// a drag beyond the small magnitude threshold triggers a highlight instead
						m_DragMagnitude = (m_DragStartVector - value).magnitude;

						m_InputMatrix = value;
						m_InputDirection = Mathf.Atan2(m_InputMatrix.y, m_InputMatrix.x) * Mathf.Rad2Deg;
						m_InputDirection += m_InputPhaseOffset;

						if (m_DragMagnitude < 0.4f) // only begin new highlight phase if magnitude has passed the minimum threshold for a select/tap
							return;

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
							{
								buttonMinMaxRange.Key.highlighted = false;
							}
						}
					}

				}
			}
		}

		public bool highlighted
		{
			set
			{
				if (!value)
				{
					Debug.LogError("<color=cyan>ending highlight with a magnitude value of : </color>" + value);
					m_DragMagnitude = 0f;
					m_DragStartVector = new Vector2();
					m_DragSelectMaxTime = 0f;
					m_InputDirection = 0f;

					if (!m_HighlightedButton)
						return;
					else
					{
						Debug.LogError("<color=blue>Disable any highlights occurring on menu buttons here</color>");
						m_HighlightedButton = null;

						foreach (var buttonMinMaxRange in buttonRotationRange)
							buttonMinMaxRange.Key.highlighted = false;
					}
				}
			}
		}

		public Action selectItem { private get; set; }

		void Start()
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

		void SetupRadialSlotPositions()
		{
			const float kRotationSpacing = 22.5f;
			for (int i = 0; i < kSlotCount; ++i)
			{
				var slot = m_RadialMenuSlots[i];
				slot.visibleLocalRotation = Quaternion.AngleAxis(kRotationSpacing * i, Vector3.up);

				var direction = i > 7 ? -1 : 1;
				buttonRotationRange.Add(slot, new Vector2(direction * Mathf.PingPong(kRotationSpacing * i, 180f), direction * Mathf.PingPong(kRotationSpacing * i + kRotationSpacing, 180f)));

				var range = Vector2.zero;
				buttonRotationRange.TryGetValue(m_RadialMenuSlots[i], out range);

				slot.Hide();
			}

			if (m_HideCoroutine != null)
				StopCoroutine(m_HideCoroutine);

			m_HideCoroutine = StartCoroutine(AnimateHide());
		}

		IEnumerator AnimateShow()
		{
			m_SlotsMask.gameObject.SetActive(true);

			var gradientPair = UnityBrandColorScheme.GetRandomGradient();
			for (int i = 0; i < m_Actions.Count; ++i)
			{
				// prevent more actions being added beyond the max slot count
				if (i >= kSlotCount)
					break;

				var action = m_Actions[i].action;
				var slot = m_RadialMenuSlots[i];
				slot.gradientPair = gradientPair;
				slot.icon = action.icon ?? m_MissingActionIcon;

				slot.button.onClick.RemoveAllListeners();
				slot.button.onClick.AddListener(() =>
				{
					// Having to grab the index because of incorrect closure support
					var index = m_RadialMenuSlots.IndexOf(m_HighlightedButton);
					if (index == -1)
						return;

					var selectedSlot = m_RadialMenuSlots[index];
					var buttonAction = m_Actions[index].action;
					buttonAction.ExecuteAction();
					selectedSlot.icon = buttonAction.icon ?? m_MissingActionIcon;
				});
			}

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

			m_ShowCoroutine = null;
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
			m_HideCoroutine = null;
		}
	}
}
