using System.Collections;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class KeyboardMallet : MonoBehaviour
{
	[SerializeField]
	private Transform m_StemOrigin;

	[SerializeField]
	private float m_StemLength = 0.06f;

	[SerializeField]
	private float m_StemWidth = 0.003125f;

	[SerializeField]
	private Transform m_Bulb;

	[SerializeField]
	private float m_BulbRadius;

	[SerializeField]
	private Collider m_BulbCollider;

	private enum State
	{
		Visible,
		Transitioning,
		Hidden
	}

	private State m_State = State.Visible;

	private Vector3 m_BulbBaseScale;

	private KeyboardButton m_CurrentButton;

	/// <summary>
	/// Invoked by the editor to update the mallet components' transform data.
	/// </summary>
	public void UpdateMalletDimensions()
	{
		m_StemOrigin.localScale = new Vector3(m_StemWidth, m_StemLength, m_StemWidth);

		m_Bulb.transform.localPosition = new Vector3(0f, 0f, m_StemLength * 2f);
		m_Bulb.transform.localScale = Vector3.one * m_BulbRadius * 2f;
		m_BulbBaseScale = m_Bulb.transform.localScale;
	}

	/// <summary>
	/// Hide the mallet with a transition.
	/// </summary>
	public void Hide()
	{
		if (isActiveAndEnabled)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(HideMallet());
		}
	}

	/// <summary>
	/// Show the mallet with a transition.
	/// </summary>
	public void Show()
	{
		if (isActiveAndEnabled)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(ShowMallet());
		}
	}

	/// <summary>
	/// Check for colliders that are keyboard keys.
	/// </summary>
	public void CheckForKeyCollision()
	{
		if (m_State != State.Visible) return;

		if (m_CurrentButton != null)
			m_CurrentButton.OnTriggerStay(m_BulbCollider);

		var shortestDistance = Mathf.Infinity;
		KeyboardButton hitKey = null;
		Collider[] hitColliders = Physics.OverlapSphere(m_Bulb.position, m_BulbRadius);
		foreach (var col in hitColliders)
		{
			var key = col.GetComponentInParent<KeyboardButton>();
			if (key != null)
			{
				var newDist = Vector3.Distance(m_Bulb.position, key.transform.position);
				if (newDist < shortestDistance)
					hitKey = key;
			}
		}

		if (m_CurrentButton != hitKey)
		{
			if (m_CurrentButton != null)
				m_CurrentButton.OnTriggerExit(m_BulbCollider);

			m_CurrentButton = hitKey;

			if (m_CurrentButton != null)
				m_CurrentButton.OnTriggerEnter(m_BulbCollider);
		}
	}

	private void Awake()
	{
		m_BulbBaseScale = m_Bulb.localScale;
	}

	private IEnumerator HideMallet()
	{
		m_State = State.Transitioning;

		var stemScale = m_StemOrigin.localScale;
		var currentLength = m_StemOrigin.localScale.y; // cache current length for smooth animation to target value without snapping
			
		const float kSmoothTime = 0.1875f;
		var startTime = Time.realtimeSinceStartup;
		float smoothVelocity = 0f;
		while (Time.realtimeSinceStartup < startTime + kSmoothTime)
		{
			currentLength = U.Math.SmoothDamp(currentLength, 0f, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
			m_StemOrigin.localScale = new Vector3(stemScale.x, currentLength, stemScale.z);
			m_Bulb.transform.localPosition = new Vector3(0f, 0f, currentLength * 2f);
			m_Bulb.transform.localScale = m_BulbBaseScale * currentLength;

			yield return null;
		}

		m_Bulb.transform.localScale = Vector3.zero;

		m_State = State.Hidden;
	}

	private IEnumerator ShowMallet()
	{
		m_State = State.Transitioning;

		var stemScale = m_StemOrigin.localScale;
		var currentLength = m_StemOrigin.localScale.y;

		const float kSmoothTime = 0.3125f;
		var startTime = Time.realtimeSinceStartup;
		float smoothVelocity = 0f;
		while (Time.realtimeSinceStartup < startTime + kSmoothTime)
		{
			currentLength = U.Math.SmoothDamp(currentLength, m_StemLength, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
			m_StemOrigin.localScale = new Vector3(stemScale.x, currentLength, stemScale.z);
			m_Bulb.transform.localPosition = new Vector3(0f, 0f, currentLength * 2f);
			m_Bulb.transform.localScale = m_BulbBaseScale * currentLength;
			yield return null;
		}

		m_Bulb.transform.localScale = m_BulbBaseScale;

		// only set the value if another transition hasn't begun
		m_State = State.Visible;
	}
}
