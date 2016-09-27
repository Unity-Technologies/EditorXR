using System.Collections;
using UnityEngine;
using Valve.VR;

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

	private State m_State;

	private Vector3 m_BulbStartScale;

	// TODO replace this logic with physics once that's working
	private KeyboardButton m_CurrentButton;
	private KeyboardButton currentButton
	{
		get { return m_CurrentButton; }
		set
		{
			if (m_CurrentButton == value) return;

			if (m_CurrentButton != null)
			{
				m_CurrentButton.OnTriggerExit(m_BulbCollider);
			}

			m_CurrentButton = value;

			if (m_CurrentButton != null)
			{
				m_CurrentButton.OnTriggerEnter(m_BulbCollider);
			}
		}
	}

	public void UpdateMalletDimensions()
	{
		m_StemOrigin.localScale = new Vector3(m_StemWidth, m_StemLength, m_StemWidth);

		m_Bulb.transform.localPosition = new Vector3(0f, 0f, m_StemLength * 2f);
		m_Bulb.transform.localScale = Vector3.one * m_BulbRadius * 2f;
		m_BulbStartScale = m_Bulb.transform.localScale;
	}


	public void Hide()
	{
		if (isActiveAndEnabled)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(HideMallet());
		}
	}

	public void Show()
	{
		if (isActiveAndEnabled)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(ShowMallet());
		}
	}

	private void Start()
	{
		m_BulbStartScale = m_Bulb.localScale;
		m_State = State.Visible;
	}

	private void Update()
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
		currentButton = hitKey;
	}

	private IEnumerator HideMallet()
	{
		m_State = State.Transitioning;

		var stemScale = m_StemOrigin.localScale;
		// cache current width for smooth animation to target value without snapping
		var currentLength = m_StemOrigin.localScale.y;
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
		var currentLength = m_StemOrigin.localScale.y;
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

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		//        var pos = selector.directSelectTransform ? selector.directSelectTransform.position : transform.position;
		Gizmos.DrawWireSphere(m_Bulb.position, m_BulbRadius);
	}
}
