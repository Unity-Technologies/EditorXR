using UnityEngine;
using UnityEngine.VR.Utilities;

//Q: Should combine with ChessboardWorkspace?
public class Chessboard : MonoBehaviour
{
	public Matrix4x4 Matrix
	{
		get
		{
			Vector3 centerOffset = clipCenter.position;
			centerOffset.y = 0f; // we only need to offset in x/z, so the center of the miniworld is in sync with the clipcenter
			Matrix4x4 clipOffsetMatrix = Matrix4x4.TRS(centerOffset, Quaternion.identity, Vector3.one);
			return transform.localToWorldMatrix * clipOffsetMatrix.inverse;
		}
	}

	public Transform clipCenter = null;
	public float[] clipDistances = new float[kPlaneCount];
	public RectTransform clipRect = null;
	public LayerMask rendererCullingMask = -1;

	private const float k_UIScale = 500;            //Set this to whatever your height/width is in the prefab
	private static readonly int kPlaneCount = 6;

	[SerializeField]
	private Transform boundsCube;

	private ChessboardRenderer miniRenderer = null;
	private float m_YBounds = 1;

	internal void SetBounds(Bounds bounds)
	{
		clipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x * k_UIScale);
		clipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bounds.size.z * k_UIScale);
		boundsCube.transform.localScale = bounds.size;
		boundsCube.transform.localPosition = Vector3.up * bounds.extents.y;
		m_YBounds = bounds.size.y;
	}

	public void MoveForward()
	{
		clipCenter.Translate(Vector3.forward);
	}

	public void MoveBackward()
	{
		clipCenter.Translate(Vector3.back);
	}

	public void MoveLeft()
	{
		clipCenter.Translate(Vector3.left);
	}

	public void MoveRight()
	{
		clipCenter.Translate(Vector3.right);
	}

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

		for (int i = 0; i < kPlaneCount; i++)
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
			SetBounds(new Bounds(Vector3.zero, Vector3.one));
		}

		Camera main = U.Camera.GetMainCamera();
		miniRenderer = main.gameObject.AddComponent<ChessboardRenderer>();
		miniRenderer.miniWorld = this;
		miniRenderer.cullingMask = rendererCullingMask;
		if (U.Object.IsEditModeActive(this))
			miniRenderer.runInEditMode = true;

		// Sync with where camera is initially
		Transform pivot = U.Camera.GetViewerPivot();
		clipCenter.transform.position = pivot.transform.position;
	}

	private void OnDisable()
	{
		if ((clipCenter.hideFlags & HideFlags.DontSave) != 0)
			U.Object.Destroy(clipCenter.gameObject);

		if (miniRenderer)
			U.Object.Destroy(miniRenderer);
	}

	private void LateUpdate()
	{
		// Adjust clip distances, which are in world coordinates to match with the UI clip rect
		Vector3[] fourCorners = new Vector3[4]; // Clock-wise from bottom-left corner
		clipRect.GetWorldCorners(fourCorners);

		Vector3 center = transform.InverseTransformPoint(clipRect.position);
		for (int i = 0; i < 4; i++)
			fourCorners[i] = transform.InverseTransformPoint(fourCorners[i]);

		// Clip distances are distance of the plane from the center location in each direction
		clipDistances[0] = Mathf.Abs((fourCorners[0] - center).x);
		clipDistances[1] = Mathf.Abs((fourCorners[1] - center).z);
		clipDistances[2] = Mathf.Abs((fourCorners[2] - center).x);
		clipDistances[3] = Mathf.Abs((fourCorners[3] - center).z);
		clipDistances[4] = m_YBounds - center.y;
		clipDistances[5] = 0;							 
	}
}
