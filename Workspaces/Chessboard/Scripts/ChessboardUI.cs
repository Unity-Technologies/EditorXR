using UnityEngine;
using UnityEngine.VR.Handles;

public class ChessboardUI : MonoBehaviour
{
	public Renderer grid { get { return m_Grid; } }
	[SerializeField]
	private Renderer m_Grid;

	public DirectHandle panZoomHandle { get { return m_PanZoomHandle; } }
	[SerializeField]
	private DirectHandle m_PanZoomHandle;
}