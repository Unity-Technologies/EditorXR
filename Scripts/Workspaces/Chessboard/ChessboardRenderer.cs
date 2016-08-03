using UnityEngine;	 
using UnityEngine.VR.Utilities;

public class ChessboardRenderer : MonoBehaviour
{
	public Chessboard miniWorld { private get; set; }
	public LayerMask cullingMask { private get; set; }

	private Camera m_MainCamera = null;
	private Camera m_MiniCamera = null;
	private bool m_RenderingMiniWorlds = false;

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

			if (m_MainCamera && miniWorld && miniWorld.clipCenter)
			{
				m_MiniCamera.CopyFrom(m_MainCamera);

				m_MiniCamera.cullingMask = cullingMask;
				m_MiniCamera.clearFlags = CameraClearFlags.Nothing;
				m_MiniCamera.worldToCameraMatrix = m_MainCamera.worldToCameraMatrix * miniWorld.matrix;
				Shader shader = Shader.Find("Custom/Custom Clip Planes");
				//mainCamera.SetReplacementShader(shader, null);
				if (miniWorld.clipCenter)
				{
					Shader.SetGlobalVector("_ClipCenter", miniWorld.clipCenter.position);
					//Shader.SetGlobalFloat("_ClipDistance", miniWorld.clipDistance);
					for (int i = 0; i < 6; i++)
						Shader.SetGlobalFloat("_ClipDistance" + i.ToString(), miniWorld.clipDistances[i]);
				}
				m_MiniCamera.RenderWithShader(shader, string.Empty);
			}

			m_RenderingMiniWorlds = false;
		}
	}
}