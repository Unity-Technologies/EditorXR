using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Helpers;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;
using UnityEngine.VR.Extensions;

public class KeyboardButton : BaseHandle
{
	protected enum SelectionState
	{
		Normal,
		Highlighted,
		Pressed,
		Disabled
	}

	private const float kRepeatTime = 0.35f;
	private const float kRepeatDecayFactor = 0.75f;
	private const float kClickTime = 0.3f;
	private const float kPressEmission = 1f;
	private const float kEmissionLerpTime = 0.1f;
	private const float kKeyResponseDuration = 0.5f;
	private const float kKeyResponseAmplitude = 0.06f;

	public Text textComponent { get { return m_TextComponent; } set { m_TextComponent = value; } }

	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private char m_Character;

	[SerializeField]
	private bool m_UseShiftCharacter;

	[SerializeField]
	private char m_ShiftCharacter;

	public bool shiftMode { get { return m_ShiftMode; } }

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

	[SerializeField]
	WorkspaceButton m_WorkspaceButton;

	float m_HoldStartTime;
	float m_RepeatWaitTime;
	float m_PressDownTime;
	bool m_Holding;
	bool m_Triggered;
	Material m_TargetMeshMaterial;
	Coroutine m_ChangeEmissionCoroutine;
	Coroutine m_PunchKeyCoroutine;

	Action<char> m_KeyPress;
	Func<bool> m_PressOnHover;
	Func<bool> m_InTransition; 

	public Color targetMeshBaseColor
	{
		get { return m_TargetMeshBaseColor; }
	}

	Color m_TargetMeshBaseColor;

	public Material targetMeshMaterial
	{
		get { return m_TargetMeshMaterial; }
	}

	public CanvasGroup canvasGroup
	{
		get
		{
			return !m_CanvasGroup
				? GetComponentInChildren<CanvasGroup>(true)
				: m_CanvasGroup;
		}
		private set
		{
			m_CanvasGroup = value;
		}
	}

	CanvasGroup m_CanvasGroup;

	public SmoothMotion smoothMotion { get; set; }

	void Awake()
	{
		var targetMeshTransform = m_TargetMesh.transform;
		m_TargetMeshInitialLocalPosition = targetMeshTransform.localPosition;
		m_TargetMeshInitialScale = targetMeshTransform.localScale;
		m_TargetMeshMaterial = U.Material.GetMaterialClone(m_TargetMesh.GetComponent<Renderer>());
		m_TargetMeshBaseColor = m_TargetMeshMaterial.color;

		smoothMotion = GetComponent<SmoothMotion>();
		smoothMotion.enabled = false;
	}

	/// <summary>
	/// Initiallize this key
	/// </summary>
	/// <param name="keyPress">Method to be invoked when the key is pressed</param>
	/// <param name="pressOnHover">Method to be invoked to determine key behaviour</param>
	public void Setup(Action<char> keyPress, Func<bool> pressOnHover, Func<bool> inTransition)
	{
		m_PressOnHover = pressOnHover;
		m_KeyPress = keyPress;
		m_InTransition = inTransition;
	}

	/// <summary>
	/// Enable or disable shift mode for this key
	/// </summary>
	/// <param name="active">Set to true to enable shift, false to disable</param>
	public void SetShiftModeActive(bool active)
	{
		if (!m_UseShiftCharacter) return;

		m_ShiftMode = active;

		if (textComponent != null)
		{
			if (m_ShiftMode && m_ShiftCharacter != 0)
			{
				textComponent.text = m_ShiftCharacter.ToString();
			}
			else
			{
				if (textComponent.text.Length > 1)
					textComponent.text = textComponent.text.ToLower();
				else
					textComponent.text = m_Character.ToString();
			}
		}
	}

	protected override void OnHandleHoverStarted(HandleEventData eventData)
	{
		base.OnHandleHoverStarted(eventData);

		if (!m_PressOnHover() && !m_InTransition())
		{
			m_WorkspaceButton.highlight = true;
		}
	}

	protected override void OnHandleHoverEnded(HandleEventData eventData)
	{
		base.OnHandleHoverEnded(eventData);

		m_WorkspaceButton.highlight = false;
	}

	protected override void OnHandleDragStarted(HandleEventData eventData)
	{
		if (!m_PressOnHover())
		{
			m_PressDownTime = Time.realtimeSinceStartup;

			if (m_RepeatOnHold)
				KeyPressed();
		}

		base.OnHandleDragStarted(eventData);
	}

	protected override void OnHandleDragging(HandleEventData eventData)
	{
		if (!m_PressOnHover())
		{
			if (m_RepeatOnHold)
				HoldKey();
		}

		base.OnHandleDragging(eventData);
	}

	protected override void OnHandleDragEnded(HandleEventData eventData)
	{
		if (!m_PressOnHover())
		{
			if (m_RepeatOnHold)
				EndKeyHold();
			else if (Time.realtimeSinceStartup - m_PressDownTime < kClickTime)
				KeyPressed();
		}

		base.OnHandleDragEnded(eventData);
	}

	public void OnTriggerEnter(Collider col)
	{
		if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null || m_InTransition())
			return;

		if (transform.InverseTransformPoint(col.transform.position).z > 0f)
			return;
		else
			m_Triggered = true;

		KeyPressed();
	}

	public void OnTriggerStay(Collider col)
	{
		if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null || m_InTransition())
			return;

		if (m_RepeatOnHold && m_Triggered)
			HoldKey();
	}

	public void OnTriggerExit(Collider col)
	{
		if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null)
			return;

		if (m_RepeatOnHold && m_Triggered)
			EndKeyHold();

		m_Triggered = false;
	}

	private void KeyPressed()
	{
		if (m_KeyPress == null || m_InTransition()) return;

		if (m_ShiftMode && m_ShiftCharacter != 0)
			m_KeyPress(m_ShiftCharacter);
		else
			m_KeyPress(m_Character);

		// Return since the escape key will disable the keyboard
		if ((!shiftMode && (KeyCode)m_Character == KeyCode.Escape) || (shiftMode && (KeyCode)m_ShiftCharacter == KeyCode.Escape)) // Avoid message about starting coroutine on inactive object
		{
			var targetMeshTransform = m_TargetMesh.transform;
			targetMeshTransform.localScale = m_TargetMeshInitialScale;
			targetMeshTransform.localPosition = m_TargetMeshInitialLocalPosition;
			return;
		}

		this.StopCoroutine(ref m_ChangeEmissionCoroutine);
		m_ChangeEmissionCoroutine = StartCoroutine(IncreaseEmission());

		this.StopCoroutine(ref m_PunchKeyCoroutine);
		m_PunchKeyCoroutine = StartCoroutine(PunchKey());

		if (m_RepeatOnHold)
			StartKeyHold();
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
			m_RepeatWaitTime *= kRepeatDecayFactor;
		}
	}

	private void EndKeyHold()
	{
		m_Holding = false;

		this.StopCoroutine(ref m_ChangeEmissionCoroutine);
		m_ChangeEmissionCoroutine = StartCoroutine(DecreaseEmission());
	}

	private void OnDisable()
	{
		InstantClearState();
	}

	protected virtual void InstantClearState()
	{
		var finalColor = Color.white * Mathf.LinearToGammaSpace(0f);
		m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);

		m_TargetMeshMaterial.color = m_TargetMeshBaseColor;
		m_WorkspaceButton.highlight = false;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TargetMeshMaterial);
	}

	IEnumerator IncreaseEmission()
	{
		var t = 0f;
		Color finalColor;
		while (t < kEmissionLerpTime)
		{
			var emission = t / kEmissionLerpTime;
			finalColor = Color.white * Mathf.LinearToGammaSpace(emission * kPressEmission);
			m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);
			t += Time.unscaledDeltaTime;

			yield return null;
		}

		finalColor = Color.white * Mathf.LinearToGammaSpace(kPressEmission);
		m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);

		if (!m_Holding)
			m_ChangeEmissionCoroutine = StartCoroutine(DecreaseEmission());
		else
			m_ChangeEmissionCoroutine = null;
	}

	IEnumerator DecreaseEmission()
	{
		var t = 0f;
		Color finalColor;
		while (t < kEmissionLerpTime)
		{
			var emission = 1f - t / kEmissionLerpTime;
			finalColor = Color.white * Mathf.LinearToGammaSpace(emission * kPressEmission);
			m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);
			t += Time.unscaledDeltaTime;

			yield return null;
		}
		finalColor = Color.white * Mathf.LinearToGammaSpace(0f);
		m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);

		m_ChangeEmissionCoroutine = null;
	}

	IEnumerator PunchKey()
	{
		var targetMeshTransform = m_TargetMesh.transform;
		targetMeshTransform.localPosition = m_TargetMeshInitialLocalPosition;

		var elapsedTime = 0f;
		while (elapsedTime < kKeyResponseDuration)
		{
			elapsedTime += Time.unscaledDeltaTime;
			var t = Mathf.Clamp01(elapsedTime / kKeyResponseDuration);

			if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f))
				break;

			const float amplitude = 0.3f;
			t = Mathf.Pow(2f, -10f * t) * Mathf.Sin(t * (2f * Mathf.PI) / amplitude);

			targetMeshTransform.localScale = m_TargetMeshInitialScale + m_TargetMeshInitialScale * t * kKeyResponseAmplitude;

			var pos = m_TargetMeshInitialLocalPosition;
			pos.z = t * kKeyResponseAmplitude;
			targetMeshTransform.localPosition = pos;

			elapsedTime += Time.unscaledDeltaTime;
			yield return null;
		}

		targetMeshTransform.localScale = m_TargetMeshInitialScale;
		targetMeshTransform.localPosition = m_TargetMeshInitialLocalPosition;
		m_PunchKeyCoroutine = null;
	}
}