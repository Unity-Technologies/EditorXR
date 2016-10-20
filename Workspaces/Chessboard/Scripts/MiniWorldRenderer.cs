using System;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class MiniWorldRenderer : MonoBehaviour
{
	private Camera m_MainCamera;
	private Camera m_MiniCamera;
	private bool m_RenderingMiniWorlds;

	public MiniWorld miniWorld { private get; set; }
	public LayerMask cullingMask { private get; set; }

	public Func<bool> preProcessRender { private get; set; }
	public Action postProcessRender { private get; set; }

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

		m_MainCamera.cullingMask &= ~LayerMask.GetMask("MiniWorldOnly");
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

				if(preProcessRender())
					m_MiniCamera.RenderWithShader(shader, string.Empty);

				postProcessRender();
			}

			m_RenderingMiniWorlds = false;
		}
	}
}