using UnityEngine;
using UnityEngine.VR.Utilities;

//Q: Should combine with ChessboardWorkspace?
public class MiniWorld : MonoBehaviour
{
	public Matrix4x4 matrix
	{
		get
		{		
			//Q: use rotation?
			Matrix4x4 clipOffsetMatrix = Matrix4x4.TRS(clipCenter.position, Quaternion.identity, clipCenter.lossyScale);
			return transform.localToWorldMatrix * clipOffsetMatrix.inverse;
		}
	}

	public Transform clipCenter { get; private set; }
	public readonly float[] clipDistances = new float[k_PlaneCount];         //Remove readonly if you want to see clip distances in inspector

	private static readonly LayerMask s_RendererCullingMask = -1;
	private const int k_PlaneCount = 6;
	private const float k_TranslationScale = 0.1f;

	[SerializeField]
	private RectTransform m_ClipRect = null;

	private MiniWorldRenderer m_MiniWorldRenderer = null;
	private float m_YBounds = 1;

	public void MoveForward()
	{
		clipCenter.Translate(Vector3.forward * k_TranslationScale);
	}

	public void MoveBackward()
	{
		clipCenter.Translate(Vector3.back * k_TranslationScale);
	}

	public void MoveLeft()
	{
		clipCenter.Translate(Vector3.left * k_TranslationScale);
	}

	public void MoveRight()
	{
		clipCenter.Translate(Vector3.right * k_TranslationScale);
	}

	internal void SetBounds(Bounds bounds)
	{
		m_ClipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x);
		m_ClipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bounds.size.z);
		m_YBounds = bounds.size.y;
	}

	//Q: Is this still needed?
	public bool ClipPlanesContain(Vector3 worldPosition)
	{
		Vector3[] planeNormals = new Vector3[]{
			new Vector3(-1, 0, 0),
			new Vector3(0, 0, 1),
			new Vector3(1, 0, 0),
			new Vector3(0, 0, -1),
			new Vector3(0, 1, 0),
			new Vector3(0, -1, 0),
		};

		for (int i = 0; i < k_PlaneCount; i++)
		{
			if (Vector3.Dot(worldPosition - (clipCenter.position - planeNormals[i] * clipDistances[i]), planeNormals[i]) < 0f)
				return false;
		}

		return true;
	}

	private void OnEnable()
	{
		if (!clipCenter)
		{
			GameObject go = new GameObject("MiniWorldClipCenter");
			go.hideFlags = HideFlags.DontSave;
			clipCenter = go.transform;
		}

		Camera main = U.Camera.GetMainCamera();
		m_MiniWorldRenderer = main.gameObject.AddComponent<MiniWorldRenderer>();
		m_MiniWorldRenderer.miniWorld = this;
		m_MiniWorldRenderer.cullingMask = s_RendererCullingMask;
		if (U.Object.IsEditModeActive(this))
			m_MiniWorldRenderer.runInEditMode = true;

		// Sync with where camera is initially
		Transform pivot = U.Camera.GetViewerPivot();
		clipCenter.transform.position = pivot.transform.position;
	}

	private void OnDisable()
	{
		if ((clipCenter.hideFlags & HideFlags.DontSave) != 0)
			U.Object.Destroy(clipCenter.gameObject);

		if (m_MiniWorldRenderer)
			U.Object.Destroy(m_MiniWorldRenderer);
	}

	private void LateUpdate()
	{
		// Adjust clip distances, which are in world coordinates to match with the UI clip rect
		Vector3[] fourCorners = new Vector3[4]; // Clock-wise from bottom-left corner
		m_ClipRect.GetWorldCorners(fourCorners);

		Vector3 center = transform.InverseTransformPoint(m_ClipRect.position);
		for (int i = 0; i < 4; i++)
			fourCorners[i] = transform.InverseTransformPoint(fourCorners[i]);

		// Clip distances are distance of the plane from the center location in each direction
		clipDistances[0] = Mathf.Abs((fourCorners[0] - center).x) * clipCenter.lossyScale.x;
		clipDistances[1] = Mathf.Abs((fourCorners[1] - center).z) * clipCenter.lossyScale.z;
		clipDistances[2] = Mathf.Abs((fourCorners[2] - center).x) * clipCenter.lossyScale.x;
		clipDistances[3] = Mathf.Abs((fourCorners[3] - center).z) * clipCenter.lossyScale.z;
		clipDistances[4] = (m_YBounds - center.y) * clipCenter.lossyScale.y;
		clipDistances[5] = 0;
	}
}
