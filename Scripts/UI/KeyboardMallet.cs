using System.Collections;
using UnityEngine;

public class KeyboardMallet : MonoBehaviour
{
	[SerializeField]
	private Transform m_StemOrigin;

	[SerializeField]
	private float m_StemLength = 0.02f;

	[SerializeField]
	private Transform m_Bulb;

	private enum State
	{
		Visible,
		Transitioning,
		Hidden
	}

	private State m_State;
	private Vector3 m_BulbStartScale;
	private Coroutine m_Transitioning;

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
			return m_BulbStartScale.x;
		}
	}

	public void Hide()
	{
		if (isActiveAndEnabled && m_LockRayObject == null)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(HideMallet());
		}
	}

	public void Show()
	{
		if (isActiveAndEnabled && m_LockRayObject == null)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(ShowMallet());
		}
	}

	public void SetLength(float length)
	{
		if (m_State != State.Visible)
			return;

		var stemScale = m_StemOrigin.localScale;
		m_StemOrigin.localScale = new Vector3(stemScale.x, length, stemScale.z);
		m_Bulb.transform.localPosition = new Vector3(0f, 0f, length * 2f);
	}

	private void Start()
	{
		m_BulbStartScale = m_Bulb.localScale;
		m_State = State.Visible;
	}

	private IEnumerator HideMallet()
	{
		m_State = State.Transitioning;

		var stemScale = m_StemOrigin.localScale;
		// cache current width for smooth animation to target value without snapping
		float currentLength = m_StemOrigin.localScale.y;
		while (currentLength > 0)
		{
			float smoothVelocity = 0f;
			currentLength = Mathf.SmoothDamp(currentLength, 0f, ref smoothVelocity, 0.1875f, Mathf.Infinity, Time.unscaledDeltaTime);
			m_StemOrigin.localScale = new Vector3(stemScale.x, currentLength, stemScale.z);
			m_Bulb.transform.localPosition = new Vector3(0f, 0f, currentLength * 2f);
			yield return null;
		}

		m_Bulb.transform.localScale = Vector3.zero;

		m_State = State.Hidden;
	}

	private IEnumerator ShowMallet()
	{
		m_State = State.Transitioning;

		var stemScale = m_StemOrigin.localScale;
		float currentLength = m_StemOrigin.localScale.y;
		float smoothVelocity = 0f;
		while (currentLength < m_StemLength)
		{
			currentLength = Mathf.SmoothDamp(currentLength, m_StemLength, ref smoothVelocity, 0.3125f, Mathf.Infinity, Time.unscaledDeltaTime);
			m_StemOrigin.localScale = new Vector3(stemScale.x, currentLength, stemScale.z);
			m_Bulb.transform.localPosition = new Vector3(0f, 0f, currentLength * 2f);
			yield return null;
		}

		m_Bulb.transform.localScale = m_BulbStartScale;

		// only set the value if another transition hasn't begun
		m_State = State.Visible;
	}
}
