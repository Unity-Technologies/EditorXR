using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class KeyboardButton : BaseHandle
{
	public Text textComponent { get { return m_TextComponent; } set { m_TextComponent = value; } }
	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private char m_Character;

	[SerializeField]
	private bool m_UseShiftCharacter;

	[SerializeField]
	private char m_ShiftCharacter;

	[SerializeField]
	private bool m_ShiftCharIsUppercase;

	private bool m_ShiftMode;

	[SerializeField]
	private bool m_MatchButtonTextToCharacter;

	[SerializeField]
	private Renderer m_TargetMesh;

	private Vector3 m_TargetMeshInitialScale;
	private Vector3 m_TargetMeshInitialLocalPosition;

	[SerializeField]
	private Graphic m_TargetGraphic;

	[SerializeField]
	private bool m_RepeatOnHold;

	private float m_HoldStartTime;
	private float m_RepeatWaitTime;
	private bool m_Holding;
	private const float kRepeatTime = 0.5f;

	private Action<char> m_KeyPress;

	private Func<bool> m_PressOnHover;

	[SerializeField]
	private ColorBlock m_Colors = ColorBlock.defaultColorBlock;

	private Material m_TargetMeshMaterial;

	protected enum SelectionState
	{
		Normal,
		Highlighted,
		Pressed,
		Disabled
	}

	private const float kPressEmission = 1f;
	private const float kEmissionLerpTime = 0.1f;
	private const float kKeyResponseDuration = 0.5f;
	private const float kKeyResponseAmplitude = 0.06f;

	private void Awake()
	{
		if (!m_TargetMesh)
			m_TargetMesh = GetComponentInChildren<Renderer>(true);
	}

	public void Setup(Action<char> keyPress, Func<bool> pressOnHover)
	{
		m_PressOnHover = pressOnHover;

		m_KeyPress = keyPress;
	}

	public void SetShiftModeActive(bool active)
	{
		if (!m_UseShiftCharacter) return;

		m_ShiftMode = active;

		if (m_TextComponent != null)
		{
			if (m_ShiftMode)
			{
				if (m_ShiftCharIsUppercase || m_TextComponent.text.Length > 1)
					m_TextComponent.text = m_TextComponent.text.ToUpper();
				else if (m_ShiftCharacter != 0)
					m_TextComponent.text = m_ShiftCharacter.ToString();
			}
			else
			{
				if (m_TextComponent.text.Length > 1)
					m_TextComponent.text = m_TextComponent.text.ToLower();
				else
					m_TextComponent.text = m_Character.ToString();
			}

			m_TextComponent.enabled = false;
			m_TextComponent.enabled = true;
		}
	}

	protected override void OnHandleHoverStarted(HandleEventData eventData)
	{
		DoGraphicStateTransition(SelectionState.Highlighted, false);

		base.OnHandleHoverStarted(eventData);
	}

	protected override void OnHandleHoverEnded(HandleEventData eventData)
	{
		DoGraphicStateTransition(SelectionState.Highlighted, false);

		base.OnHandleHoverEnded(eventData);
	}

	protected override void OnHandleDragStarted(HandleEventData eventData)
	{
		if (m_PressOnHover())
			return;

		KeyPressed();

		base.OnHandleDragStarted(eventData);
	}

	protected override void OnHandleDragging(HandleEventData eventData)
	{
		if (m_PressOnHover())
			return;

		if (m_RepeatOnHold)
			HoldKey();

		base.OnHandleDragging(eventData);
	}

	protected override void OnHandleDragEnded(HandleEventData eventData)
	{
		if (m_PressOnHover())
			return;

		if (m_RepeatOnHold)
			EndKeyHold();

		base.OnHandleDragEnded(eventData);
	}

	public void OnTriggerEnter(Collider col)
	{
		if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null)
			return;

		KeyPressed();
	}

	public void OnTriggerStay(Collider col)
	{
		if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null)
			return;

		if (m_RepeatOnHold)
			HoldKey();
	}

	public void OnTriggerExit(Collider col)
	{
		if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null)
			return;

		if (m_RepeatOnHold)
			EndKeyHold();
	}
	public void KeyPressed()
	{
		if (m_KeyPress == null) return;

		DoGraphicStateTransition(SelectionState.Pressed, false);

		if (m_ShiftMode && !m_ShiftCharIsUppercase && m_ShiftCharacter != 0)
			m_KeyPress(m_ShiftCharacter);
		else
			m_KeyPress(m_Character);

		StartCoroutine(IncreaseEmissionCoroutine());

		if (m_RepeatOnHold)
			StartKeyHold();
		else
			DoGraphicStateTransition(SelectionState.Normal, false);
	}

	private void StartKeyHold()
	{
		m_Holding = true;
		m_HoldStartTime = Time.realtimeSinceStartup;
		m_RepeatWaitTime = kRepeatTime;
	}

	private void HoldKey()
	{
		if (m_Holding && m_HoldStartTime + m_RepeatWaitTime < Time.realtimeSinceStartup)
		{
			KeyPressed();
			m_HoldStartTime = Time.realtimeSinceStartup;
			m_RepeatWaitTime *= 0.75f;
		}
	}

	private void EndKeyHold()
	{
		m_Holding = false;
		DoGraphicStateTransition(SelectionState.Normal, false);
		StartCoroutine(DecreaseEmissionCoroutine());
	}

	private void Start()
	{
		if (m_TargetMesh != null)
		{
			var targetTransform = m_TargetMesh.transform;
			m_TargetMeshInitialLocalPosition = targetTransform.localPosition;
			m_TargetMeshInitialScale = targetTransform.localScale;
			m_TargetMeshMaterial = U.Material.GetMaterialClone(m_TargetMesh);
		}
	}

	private void OnDisable()
	{
		InstantClearState();
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TargetMeshMaterial);
	}

	protected virtual void DoGraphicStateTransition(SelectionState state, bool instant)
	{
		Color graphicTintColor;

		switch (state)
		{
			case SelectionState.Normal:
				graphicTintColor = m_Colors.normalColor;
				break;
			case SelectionState.Highlighted:
				graphicTintColor = m_Colors.highlightedColor;
				break;
			case SelectionState.Pressed:
				graphicTintColor = m_Colors.pressedColor;
				StartCoroutine(PunchKey());
				break;
			case SelectionState.Disabled:
				graphicTintColor = m_Colors.disabledColor;
				break;
			default:
				graphicTintColor = Color.black;
				break;
		}

		if (gameObject.activeInHierarchy)
			StartGraphicColorTween(graphicTintColor * m_Colors.colorMultiplier, instant);
	}

	private void StartGraphicColorTween(Color targetColor, bool instant)
	{
		if (m_TargetGraphic == null)
			return;

		m_TargetGraphic.CrossFadeColor(targetColor, instant ? 0f : m_Colors.fadeDuration, true, true);
	}

	protected virtual void InstantClearState()
	{
		DoGraphicStateTransition(SelectionState.Normal, true);
	}

	private IEnumerator IncreaseEmissionCoroutine()
	{
		if (!gameObject.activeInHierarchy) yield break;

		StopCoroutine("DecreaseEmissionCoroutine");

		var t = 0f;
		Color finalColor;
		while (t < kEmissionLerpTime)
		{
			float emission = Mathf.PingPong(t / kEmissionLerpTime, kPressEmission);
			finalColor = Color.white * Mathf.LinearToGammaSpace(emission);
			m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);
			t += Time.unscaledDeltaTime;

			yield return null;
		}
		finalColor = Color.white * Mathf.LinearToGammaSpace(kPressEmission);
		m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);

		if (!m_Holding)
			StartCoroutine(DecreaseEmissionCoroutine());
	}

	private IEnumerator DecreaseEmissionCoroutine()
	{
		if (!gameObject.activeInHierarchy) yield break;

		StopCoroutine("IncreaseEmissionCoroutine");

		var t = 0f;
		Color finalColor;
		while (t < kEmissionLerpTime)
		{
			float emission = Mathf.PingPong(1f - t / kEmissionLerpTime, kPressEmission);
			finalColor = Color.white * Mathf.LinearToGammaSpace(emission);
			m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);
			t += Time.unscaledDeltaTime;

			yield return null;
		}
		finalColor = Color.white * Mathf.LinearToGammaSpace(0f);
		m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);
	}

	private IEnumerator PunchKey()
	{
		var targetTransform = m_TargetMesh.transform;
		targetTransform.localPosition = m_TargetMeshInitialLocalPosition;

		var elapsedTime = 0f;
		while (elapsedTime < kKeyResponseDuration)
		{
			elapsedTime += Time.unscaledDeltaTime;
			var t = Mathf.Clamp01(elapsedTime / kKeyResponseDuration);

			if (t == 0 || t == 1)
				break;
			var p = 0.3f;
			t = Mathf.Pow(2, -10 * t) * Mathf.Sin(t * (2 * Mathf.PI) / p);

			targetTransform.localScale = m_TargetMeshInitialScale + m_TargetMeshInitialScale * t * kKeyResponseAmplitude;

			var pos = m_TargetMeshInitialLocalPosition;
			pos.z = t * kKeyResponseAmplitude;
			targetTransform.localPosition = pos;

			elapsedTime += Time.unscaledDeltaTime;
			yield return null;
		}

		targetTransform.localScale = m_TargetMeshInitialScale;
		targetTransform.localPosition = m_TargetMeshInitialLocalPosition;
	}
}
