using UnityEngine;
using UnityEngine.VR.Handles;

public class ChessboardSceneObjects : MonoBehaviour
{
	public Renderer grid { get { return m_Grid; } }
	[SerializeField]
	private Renderer m_Grid;

	public DirectHandle controlBox { get { return m_ControlBox; } }
	[SerializeField]
	private DirectHandle m_ControlBox;
}