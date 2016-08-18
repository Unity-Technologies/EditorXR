using UnityEngine;
using UnityEngine.VR.Utilities;

public class MiniWorldRenderer : MonoBehaviour
{
	private static readonly LayerMask s_RendererCullingMask = -1;

	private Camera m_MainCamera;
	private Camera m_MiniCamera;
	private bool m_RenderingMiniWorlds;

	public MiniWorld miniWorld { private get; set; }

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

				m_MiniCamera.cullingMask = s_RendererCullingMask;
				m_MiniCamera.clearFlags = CameraClearFlags.Nothing;
				m_MiniCamera.worldToCameraMatrix = m_MainCamera.worldToCameraMatrix * miniWorld.miniToReferenceMatrix;
				Shader shader = Shader.Find("Custom/Custom Clip Planes");
				Shader.SetGlobalVector("_ClipCenter", miniWorld.referenceBounds.center);
				Shader.SetGlobalVector("_ClipExtents", miniWorld.referenceBounds.extents);
				m_MiniCamera.RenderWithShader(shader, string.Empty);
			}

			m_RenderingMiniWorlds = false;
		}
	}
}