using UnityEngine;
using UnityEngine.VR.Utilities;

//Q: Should combine with ChessboardWorkspace?
public class MiniWorld : MonoBehaviour
{
	public Matrix4x4 matrix
	{
		get { return transform.localToWorldMatrix * referenceTransform.worldToLocalMatrix; }
	}

	public Transform referenceTransform { get; private set; }
	public Bounds referenceBounds
	{
		get
		{
			return new Bounds(referenceTransform.position, Vector3.Scale(referenceTransform.localScale, m_LocalBoundsSize));
		}
		set
		{
			referenceTransform.position = value.center;
			m_LocalBoundsSize = Vector3.Scale(Inverse(referenceTransform.localScale), value.size);
		}
	}
	public Bounds localReferenceBounds
	{
		get { return new Bounds(Vector3.zero, m_LocalBoundsSize); }
		set { m_LocalBoundsSize = value.size; }
	}
	private Vector3 m_LocalBoundsSize = Vector3.one;

	private static readonly LayerMask s_RendererCullingMask = -1;
	private const float kTranslationScale = 0.1f;

	private MiniWorldRenderer m_MiniWorldRenderer;

	public void MoveForward()
	{
		referenceTransform.Translate(Vector3.forward * kTranslationScale);
	}

	public void MoveBackward()
	{
		referenceTransform.Translate(Vector3.back * kTranslationScale);
	}

	public void MoveLeft()
	{
		referenceTransform.Translate(Vector3.left * kTranslationScale);
	}

	public void MoveRight()
	{
		referenceTransform.Translate(Vector3.right * kTranslationScale);
	}

	internal void SetBounds(Bounds bounds)
	{											  
		localReferenceBounds = bounds;
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
		if (!referenceTransform)
			referenceTransform = new GameObject("MiniWorldReference") { hideFlags = HideFlags.DontSave }.transform;

		Camera main = U.Camera.GetMainCamera();
		m_MiniWorldRenderer = main.gameObject.AddComponent<MiniWorldRenderer>();
		m_MiniWorldRenderer.miniWorld = this;
		m_MiniWorldRenderer.cullingMask = s_RendererCullingMask;
		if (U.Object.IsEditModeActive(this))
			m_MiniWorldRenderer.runInEditMode = true;

		// Sync with where camera is initially
		Transform pivot = U.Camera.GetViewerPivot();
		referenceTransform.position = pivot.transform.position;
	}

	private void OnDisable()
	{
		if ((referenceTransform.hideFlags & HideFlags.DontSave) != 0)
			U.Object.Destroy(referenceTransform.gameObject);

		if (m_MiniWorldRenderer)
			U.Object.Destroy(m_MiniWorldRenderer);
	}
	//TODO: Add this function to U.Math after Spatial Hash merge
	static Vector3 Inverse(Vector3 vec)
	{
		return new Vector3(1 / vec.x, 1 / vec.y, 1 / vec.z);
	}
}
