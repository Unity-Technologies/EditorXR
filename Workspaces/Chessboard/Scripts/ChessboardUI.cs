using UnityEngine;
using UnityEngine.VR.Handles;

public class ChessboardUI : MonoBehaviour
{
	public Renderer grid { get { return m_Grid; } }
	[SerializeField]
	private Renderer m_Grid;

	public BaseHandle panZoomHandle { get { return m_PanZoomHandle; } }
	[SerializeField]
	private BaseHandle m_PanZoomHandle;
}