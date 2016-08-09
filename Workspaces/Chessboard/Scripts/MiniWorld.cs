using UnityEngine;
using UnityEngine.VR.Utilities;

//Q: Should combine with ChessboardWorkspace?
public class MiniWorld : MonoBehaviour
{
	public readonly float[] clipDistances = new float[kPlaneCount]; //Remove readonly if you want to see clip distances in inspector

	public Matrix4x4 matrix
	{
		get { return transform.localToWorldMatrix * clipBox.transform.worldToLocalMatrix; }
	}

	public ClipBox clipBox { get; private set; }

	private static readonly LayerMask s_RendererCullingMask = -1;
	private const int kPlaneCount = 6;
	private const float kTranslationScale = 0.1f;

	private MiniWorldRenderer m_MiniWorldRenderer;

	public void MoveForward()
	{
		clipBox.transform.Translate(Vector3.forward * kTranslationScale);
	}

	public void MoveBackward()
	{
		clipBox.transform.Translate(Vector3.back * kTranslationScale);
	}

	public void MoveLeft()
	{
		clipBox.transform.Translate(Vector3.left * kTranslationScale);
	}

	public void MoveRight()
	{
		clipBox.transform.Translate(Vector3.right * kTranslationScale);
	}

	internal void SetBounds(Bounds bounds)
	{											  
		clipBox.localBounds = bounds;
	}

	//Q: Is this still needed?
	//public bool ClipPlanesContain(Vector3 worldPosition)
	//{
	//	Vector3[] planeNormals = new Vector3[]
	//	{
	//		new Vector3(-1, 0, 0),
	//		new Vector3(0, 0, 1),
	//		new Vector3(1, 0, 0),
	//		new Vector3(0, 0, -1),
	//		new Vector3(0, 1, 0),
	//		new Vector3(0, -1, 0),
	//	};

	//	for (int i = 0; i < kPlaneCount; i++)
	//	{
	//		if (Vector3.Dot(worldPosition - (clipBox.position - planeNormals[i] * clipDistances[i]), planeNormals[i]) < 0f)
	//			return false;
	//	}

	//	return true;
	//}

	private void OnEnable()
	{
		if (!clipBox)
		{
			GameObject go = new GameObject("MiniWorldClipCenter");
			go.hideFlags = HideFlags.DontSave;
			clipBox = go.AddComponent<ClipBox>();
		}

		Camera main = U.Camera.GetMainCamera();
		m_MiniWorldRenderer = main.gameObject.AddComponent<MiniWorldRenderer>();
		m_MiniWorldRenderer.miniWorld = this;
		m_MiniWorldRenderer.cullingMask = s_RendererCullingMask;
		if (U.Object.IsEditModeActive(this))
			m_MiniWorldRenderer.runInEditMode = true;

		// Sync with where camera is initially
		Transform pivot = U.Camera.GetViewerPivot();
		clipBox.transform.position = pivot.transform.position;
	}

	private void OnDisable()
	{
		if ((clipBox.hideFlags & HideFlags.DontSave) != 0)
			U.Object.Destroy(clipBox.gameObject);

		if (m_MiniWorldRenderer)
			U.Object.Destroy(m_MiniWorldRenderer);
	}
}
