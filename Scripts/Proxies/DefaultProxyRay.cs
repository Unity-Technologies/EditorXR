using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class DefaultProxyRay : MonoBehaviour
{
	[SerializeField]
	private VRLineRenderer m_LineRenderer;

	[SerializeField]
	private GameObject m_Tip;

	[SerializeField]
	private float m_LineWidth;

	[SerializeField]
	private MeshFilter m_Cone;

	private enum State
	{
		Visible,
		Transitioning,
		Hidden
	}

	private State m_State;
	private Vector3 m_TipStartScale;
	Transform m_ConeTransform;
	Vector3 m_OriginalConeLocalScale;

	/// <summary>
	/// The object that is set when LockRay is called while the ray is unlocked.
	/// As long as this reference is set, and the ray is locked, only that object can unlock the ray.
	/// If the object reference becomes null, the ray will be free to show/hide/lock/unlock until another locking entity takes ownership.
	/// </summary>
	private object m_LockRayObject;

	public bool LockRay(object lockCaller)
	{
		// Allow the caller to lock the ray
		// If the reference to the lockCaller is destroyed, and the ray was not properly
		// unlocked by the original locking caller, then allow locking by another object
		if (m_LockRayObject == null)
		{
			m_LockRayObject = lockCaller;
			return true;
		}

		return false;
	}

	public bool UnlockRay(object unlockCaller)
	{
		// Only allow unlocking if the original lock caller is null or there is no locker caller set
		if (m_LockRayObject == unlockCaller)
		{
			m_LockRayObject = null;
			return true;
		}

		return false;
	}

	/// <summary>
	/// The length of the direct selection pointer
	/// </summary>
	public float pointerLength
	{
		get
		{
			return (m_Cone.transform.TransformPoint(m_Cone.sharedMesh.bounds.size.z * Vector3.forward) - m_Cone.transform.position).magnitude;
		}
	}

	public void Hide()
	{
		if (isActiveAndEnabled && m_LockRayObject == null)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();
			
			StartCoroutine(HideRay());
			StartCoroutine(HideCone());
		}
	}

	public void Show()
	{
		if (isActiveAndEnabled && m_LockRayObject == null)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();
			
			StartCoroutine(ShowRay());
			StartCoroutine(ShowCone());
		}
	}

	public void SetLength(float length)
	{
		if (m_State != State.Visible)
			return;

		m_LineRenderer.transform.localScale = Vector3.one * length;
		m_LineRenderer.SetWidth(m_LineWidth, m_LineWidth * length);
		m_Tip.transform.position = transform.position + transform.forward * length;
		m_Tip.transform.localScale = length * m_TipStartScale;
	}

	private void Awake()
	{
		m_ConeTransform = m_Cone.transform;
		m_OriginalConeLocalScale = m_ConeTransform.localScale;
	}

	private void Start()
	{
		m_TipStartScale = m_Tip.transform.localScale;
		m_State = State.Visible;
	}

	private IEnumerator HideRay()
	{
		m_State = State.Transitioning;
		m_Tip.transform.localScale = Vector3.zero;

		// cache current width for smooth animation to target value without snapping
		var currentWidth = m_LineRenderer.widthStart;
		const float kTargetWidth = 0f;
		const float kSmoothTime = 0.1875f;
		var currentDuration = 0f;
		while (currentDuration < kSmoothTime)
		{
			float smoothVelocity = 0f;
			currentWidth = U.Math.SmoothDamp(currentWidth, kTargetWidth, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
			currentDuration += Time.unscaledDeltaTime;
			m_LineRenderer.SetWidth(currentWidth, currentWidth);
			yield return null;
		}

		m_LineRenderer.SetWidth(kTargetWidth, kTargetWidth);
		m_State = State.Hidden;
	}

	private IEnumerator ShowRay()
	{
		m_State = State.Transitioning;
		m_Tip.transform.localScale = m_TipStartScale;

		var currentWidth = m_LineRenderer.widthStart;
		var smoothVelocity = 0f;
		const float kSmoothTime = 0.3125f;
		var currentDuration = 0f;
		while (currentDuration < kSmoothTime)
		{
			currentWidth = U.Math.SmoothDamp(currentWidth, m_LineWidth, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
			currentDuration += Time.unscaledDeltaTime;
			m_LineRenderer.SetWidth(currentWidth, currentWidth);
			yield return null;
		}
		
		m_LineRenderer.SetWidth(m_LineWidth, m_LineWidth);
		m_State = State.Visible;
	}

	IEnumerator HideCone()
	{
		var currentScale = m_ConeTransform.localScale;
		var smoothVelocity = Vector3.one;
		const float kSmoothTime = 0.1875f;
		var currentDuration = 0f;
		while (currentDuration < kSmoothTime)
		{
			currentScale = U.Math.SmoothDamp(currentScale, Vector3.zero, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
			currentDuration += Time.unscaledDeltaTime;
			m_ConeTransform.localScale = currentScale;
			yield return null;
		}

		m_ConeTransform.localScale = Vector3.zero;
	}

	IEnumerator ShowCone()
	{
		var currentScale = m_ConeTransform.localScale;
		var smoothVelocity = Vector3.one;
		const float kSmoothTime = 0.3125f;
		var currentDuration = 0f;
		while (currentDuration < kSmoothTime)
		{
			currentScale = Vector3.SmoothDamp(currentScale, m_OriginalConeLocalScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
			currentDuration += Time.unscaledDeltaTime;
			m_ConeTransform.localScale = currentScale;
			yield return null;
		}

		m_ConeTransform.localScale = m_OriginalConeLocalScale;
	}
}
