using UnityEngine;
using System.Collections;

public class DefaultProxyRay : MonoBehaviour
{
	[SerializeField]
	private VRLineRenderer m_LineRenderer;

	[SerializeField]
	private GameObject m_Tip;

	[SerializeField]
	private float m_LineWidth;

	private Vector3 m_TipStartScale;
	void Start()
	{
		m_TipStartScale = m_Tip.transform.localScale;
	}

	public void SetLength(float length)
	{
		m_LineRenderer.transform.localScale = Vector3.one * length;
		m_LineRenderer.SetWidth(m_LineWidth, m_LineWidth*length);
		m_Tip.transform.position = transform.position + transform.forward * length;
		m_Tip.transform.localScale = length * m_TipStartScale;
	}
}
