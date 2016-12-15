using UnityEngine;

public class ProxyHelper : MonoBehaviour
{
	public Transform rayOrigin { get { return m_RayOrigin; } }
	[SerializeField]
	private Transform m_RayOrigin;

	public Transform menuOrigin { get { return m_MenuOrigin; } }
	[SerializeField]
	private Transform m_MenuOrigin;

	public Transform alternateMenuOrigin { get { return m_AlternateMenuOrigin; } }
	[SerializeField]
	private Transform m_AlternateMenuOrigin;

	public Transform previewOrigin { get { return m_PreviewOrigin; } }
	[SerializeField]
	private Transform m_PreviewOrigin;

	public Transform meshRoot { get { return m_MeshRoot; } }
	[SerializeField]
	private Transform m_MeshRoot;
}