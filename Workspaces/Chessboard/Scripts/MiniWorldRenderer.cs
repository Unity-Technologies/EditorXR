using System.Collections.Generic;
using UnityEngine;

using UnityEngine.VR.Utilities;

public class MiniWorldRenderer : MonoBehaviour
{
	private Camera m_MainCamera;
	private Camera m_MiniCamera;
	private bool m_RenderingMiniWorlds;

	bool[] m_RendererPreviousEnable = new bool[0];

	public MiniWorld miniWorld { private get; set; }
	public LayerMask cullingMask { private get; set; }

	public List<Renderer> ignoreList
	{
		set
		{
			m_IgnoreList = value;
			if(m_IgnoreList.Count > m_RendererPreviousEnable.Length)
				m_RendererPreviousEnable = new bool[m_IgnoreList.Count];
		}
	}
	List<Renderer> m_IgnoreList = new List<Renderer>();

	private void OnEnable()
	{
		GameObject go = new GameObject("MiniWorldCamera", typeof(Camera));
		go.hideFlags = HideFlags.DontSave;
		m_MiniCamera = go.GetComponent<Camera>();
		go.SetActive(false);
		m_RenderingMiniWorlds = false;
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
		if (!m_RenderingMiniWorlds)
		{
			// If we ever support multiple mini-worlds, then we could collect them all and render them in one loop here
			m_RenderingMiniWorlds = true;

			if (m_MainCamera && miniWorld)
			{
				m_MiniCamera.CopyFrom(m_MainCamera);

				m_MiniCamera.cullingMask = cullingMask;
				m_MiniCamera.clearFlags = CameraClearFlags.Nothing;
				m_MiniCamera.worldToCameraMatrix = m_MainCamera.worldToCameraMatrix * miniWorld.miniToReferenceMatrix;
				Shader shader = Shader.Find("Custom/Custom Clip Planes");
				Shader.SetGlobalVector("_GlobalClipCenter", miniWorld.referenceBounds.center);
				Shader.SetGlobalVector("_GlobalClipExtents", miniWorld.referenceBounds.extents);

				for (var i = 0; i < m_IgnoreList.Count; i++)
				{
					var hiddenRenderer = m_IgnoreList[i];
					m_RendererPreviousEnable[i] = hiddenRenderer.enabled;
					hiddenRenderer.enabled = false;
				}

				m_MiniCamera.RenderWithShader(shader, string.Empty);

				for (var i = 0; i < m_IgnoreList.Count; i++)
				{
					m_IgnoreList[i].enabled = m_RendererPreviousEnable[i];
				}
			}

			m_RenderingMiniWorlds = false;
		}
	}
}