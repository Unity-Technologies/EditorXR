using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class MiniWorldRenderer : MonoBehaviour
{
	public const string kShowInMiniWorldTag = "ShowInMiniWorld";
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

	Camera m_MainCamera;
	Camera m_MiniCamera;

	int[] m_IgnoredObjectLayer;
	bool[] m_IgnoreObjectRendererEnabled;

	public MiniWorld miniWorld { private get; set; }
	public LayerMask cullingMask { private get; set; }

	public Matrix4x4 worldToCameraMatrix { get { return m_MainCamera.worldToCameraMatrix * miniWorld.miniToReferenceMatrix; } }

	void Awake()
	{
		s_DefaultLayer = LayerMask.NameToLayer("Default");
	}

	private void OnEnable()
	{
		GameObject go = new GameObject("MiniWorldCamera", typeof(Camera));
		go.hideFlags = HideFlags.DontSave;
		m_MiniCamera = go.GetComponent<Camera>();
		go.SetActive(false);
	}

	private void OnDisable()
	{
		U.Object.Destroy(m_MiniCamera.gameObject);
	}

	private void OnPreRender()
	{
		if (!m_MainCamera)
			m_MainCamera = U.Camera.GetMainCamera();
	}

	private void OnPostRender()
	{
		// Do not render if miniWorld scale is too low to avoid errors in the console
		if (m_MainCamera && miniWorld && miniWorld.transform.lossyScale.magnitude > kMinScale)
		{
			m_MiniCamera.CopyFrom(m_MainCamera);

			if (m_MainCamera && miniWorld)
			{
				m_MiniCamera.CopyFrom(m_MainCamera);

				m_MiniCamera.cullingMask = cullingMask;
				m_MiniCamera.clearFlags = CameraClearFlags.Nothing;
				m_MiniCamera.worldToCameraMatrix = worldToCameraMatrix;
				Shader shader = Shader.Find("Custom/Custom Clip Planes");
				Shader.SetGlobalVector("_GlobalClipCenter", miniWorld.referenceBounds.center);
				Shader.SetGlobalVector("_GlobalClipExtents", miniWorld.referenceBounds.extents);

				for (var i = 0; i < m_IgnoreList.Count; i++)
				{
					var hiddenRenderer = m_IgnoreList[i];
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
					if (hiddenRenderer.CompareTag(kShowInMiniWorldTag))
						hiddenRenderer.gameObject.layer = m_IgnoredObjectLayer[i];
					else
						m_IgnoreList[i].enabled = m_IgnoreObjectRendererEnabled[i];
				}
			}
		}
	}
}