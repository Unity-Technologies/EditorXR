using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Utilities;

[InitializeOnLoad]
[RequiresTag(kMiniWorldCameraTag)]
[RequiresTag(kShowInMiniWorldTag)]
public class MiniWorldRenderer : MonoBehaviour
{
	public const string kShowInMiniWorldTag = "ShowInMiniWorld";
	const string kMiniWorldCameraTag = "MiniWorldCamera";
	const float kMinScale = 0.001f;

	static int s_DefaultLayer;

	public List<Renderer> ignoreList
	{
		set
		{
			m_IgnoreList = value;
			var count = m_IgnoreList == null ? 0 : m_IgnoreList.Count;
			if (m_IgnoreObjectRendererEnabled == null || count > m_IgnoreObjectRendererEnabled.Length)
			{
				m_IgnoredObjectLayer = new int[count];
				m_IgnoreObjectRendererEnabled = new bool[count];
			}
		}
	}
	List<Renderer> m_IgnoreList = new List<Renderer>();

	Camera m_MiniCamera;

	int[] m_IgnoredObjectLayer;
	bool[] m_IgnoreObjectRendererEnabled;

	public MiniWorld miniWorld { private get; set; }
	public LayerMask cullingMask { private get; set; }

	public Matrix4x4 GetWorldToCameraMatrix(Camera camera)
	{
		return camera.worldToCameraMatrix * miniWorld.miniToReferenceMatrix;
	}

	void Awake()
	{
		s_DefaultLayer = LayerMask.NameToLayer("Default");
	}

	void OnEnable()
	{
		GameObject go = new GameObject("MiniWorldCamera", typeof(Camera));
		go.tag = kMiniWorldCameraTag;
		go.hideFlags = HideFlags.DontSave;
		m_MiniCamera = go.GetComponent<Camera>();
		go.SetActive(false);
		Camera.onPostRender += RenderMiniWorld;
	}

	void OnDisable()
	{
		Camera.onPostRender -= RenderMiniWorld;
		U.Object.Destroy(m_MiniCamera.gameObject);
	}

	void RenderMiniWorld(Camera camera)
	{
		// Do not render if miniWorld scale is too low to avoid errors in the console
		if (!camera.gameObject.CompareTag(kMiniWorldCameraTag) && miniWorld && miniWorld.transform.lossyScale.magnitude > kMinScale)
		{
			m_MiniCamera.CopyFrom(camera);

			m_MiniCamera.cullingMask = cullingMask;
			m_MiniCamera.clearFlags = CameraClearFlags.Nothing;
			m_MiniCamera.worldToCameraMatrix = GetWorldToCameraMatrix(camera);
			Shader shader = Shader.Find("Custom/Custom Clip Planes");
			Shader.SetGlobalVector("_GlobalClipCenter", miniWorld.referenceBounds.center);
			Shader.SetGlobalVector("_GlobalClipExtents", miniWorld.referenceBounds.extents);

			for (var i = 0; i < m_IgnoreList.Count; i++)
			{
				var hiddenRenderer = m_IgnoreList[i];
				if (!hiddenRenderer)
					continue;

				if (hiddenRenderer.CompareTag(kShowInMiniWorldTag))
				{
					m_IgnoredObjectLayer[i] = hiddenRenderer.gameObject.layer;
					hiddenRenderer.gameObject.layer = s_DefaultLayer;
				}
				else
				{
					m_IgnoreObjectRendererEnabled[i] = hiddenRenderer.enabled;
					hiddenRenderer.enabled = false;
				}
			}

			m_MiniCamera.SetReplacementShader(shader, null);
			m_MiniCamera.Render();

			for (var i = 0; i < m_IgnoreList.Count; i++)
			{
				var hiddenRenderer = m_IgnoreList[i];
				if (!hiddenRenderer)
					continue;

				if (hiddenRenderer.CompareTag(kShowInMiniWorldTag))
					hiddenRenderer.gameObject.layer = m_IgnoredObjectLayer[i];
				else
					m_IgnoreList[i].enabled = m_IgnoreObjectRendererEnabled[i];
			}
		}
	}
}
