using UnityEngine;
using System.Collections;
using UnityEngine.VR.Utilities;

public class DefaultProxyRay : MonoBehaviour
{
	[SerializeField]
	private VRLineRenderer m_LineRenderer;

	[SerializeField]
	private GameObject m_Tip;

	[SerializeField]
	private MeshFilter m_Cone;

	[SerializeField]
	private float m_LineWidth;

	private enum State
	{
		Visible,
		Transitioning,
		Hidden
	}

	private State m_State;
	private Vector3 m_TipStartScale;
	private Coroutine m_Transitioning;

	public float length
	{
		get { return m_length; }
		set
		{
			if (m_State != State.Visible)
				return;

			m_length = value;
			m_LineRenderer.transform.localScale = Vector3.one * m_length;
			m_LineRenderer.SetWidth(m_LineWidth, m_LineWidth * m_length);
			m_Tip.transform.position = transform.position + transform.forward * m_length;
			m_Tip.transform.localScale = m_length * m_TipStartScale;
		}
	}
	private float m_length;

	public float directRayLength { get; private set; }

	public void Hide()
	{
		if (isActiveAndEnabled)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(HideRay());
		}
	}

	public void Show()
	{
		if (isActiveAndEnabled)
		{
			if (m_State == State.Transitioning)
				StopAllCoroutines();

			StartCoroutine(ShowRay());
		}
	}

	private void Start()
	{
		m_TipStartScale = m_Tip.transform.localScale;
		m_State = State.Visible;
		directRayLength = (m_Cone.transform.TransformPoint(m_Cone.sharedMesh.bounds.size.z * Vector3.forward) - m_Cone.transform.position).magnitude;
	}

	private IEnumerator HideRay()
	{
		m_State = State.Transitioning;
		m_Tip.transform.localScale = Vector3.zero;

		// cache current width for smooth animation to target value without snapping
		float currentWidth = m_LineRenderer.widthStart;
		while (currentWidth > 0)
		{
			currentWidth = U.Math.Ease(currentWidth, 0f, 3, 0.0005f);
			m_LineRenderer.SetWidth(currentWidth, currentWidth);
			yield return null;
		}

		m_LineRenderer.SetWidth(0, 0);
		m_State = State.Hidden;
	}

	private IEnumerator ShowRay()
	{
		m_State = State.Transitioning;

		float currentWidth = m_LineRenderer.widthStart;

		m_Tip.transform.localScale = m_TipStartScale;

		while (currentWidth < m_LineWidth)
		{
			currentWidth = U.Math.Ease(currentWidth, m_LineWidth, 5, 0.0005f);
			m_LineRenderer.SetWidth(currentWidth, currentWidth);

			yield return null;
		}

		// only set the value if another transition hasn't begun
		m_LineRenderer.SetWidth(m_LineWidth, m_LineWidth);
		m_State = State.Visible;
	}
}
