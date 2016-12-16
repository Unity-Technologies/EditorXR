using UnityEngine;
using UnityEngine.Experimental.EditorVR.Handles;

public class MiniWorldUI : MonoBehaviour
{
	public Renderer grid { get { return m_Grid; } }
	[SerializeField]
	private Renderer m_Grid;

	public BaseHandle panZoomHandle { get { return m_PanZoomHandle; } }
	[SerializeField]
	private BaseHandle m_PanZoomHandle;

	public Transform boundsCube { get { return m_BoundsCube; } }
	[SerializeField]
	private Transform m_BoundsCube;
}