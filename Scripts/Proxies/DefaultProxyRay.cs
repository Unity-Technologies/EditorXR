using UnityEngine;
using System.Collections;

public class DefaultProxyRay : MonoBehaviour
{
	[SerializeField]
	private VRLineRenderer m_LineRenderer;

	[SerializeField]
	private GameObject m_Tip;
	

	public void SetLength(float length)
	{
		m_LineRenderer.transform.localScale = Vector3.one * length;
		m_Tip.transform.position = transform.position + transform.forward * length;
		m_Tip.transform.localScale = length * 0.01f * Vector3.one;
	}
}
