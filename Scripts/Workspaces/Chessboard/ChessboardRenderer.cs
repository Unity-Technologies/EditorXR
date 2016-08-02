using UnityEngine;
using System.Collections;
using UnityEngine.VR;
using UnityEngine.VR.Utilities;

public class MiniRenderer : MonoBehaviour
{
	public MiniWorld miniWorld = null;
	public LayerMask cullingMask = -1;

	private Camera mainCamera = null;
	private Camera miniCamera = null;
	private bool renderingMiniWorlds = false;

	private void OnEnable()
	{
		GameObject go = new GameObject("MiniWorldCamera", typeof(Camera));
		go.hideFlags = HideFlags.DontSave;
		miniCamera = go.GetComponent<Camera>();
		go.SetActive(false);
		renderingMiniWorlds = false;
	}

	private void OnDisable()
	{
		U.Object.Destroy(miniCamera.gameObject);
	}

	private void OnPreRender()
	{
		if (!mainCamera)
			mainCamera = U.Camera.GetMainCamera();

		mainCamera.cullingMask &= ~LayerMask.GetMask("MiniWorldOnly");
	}

	private void OnPostRender()
	{
		if (!renderingMiniWorlds)
		{
			// If we ever support multiple mini-worlds, then we could collect them all and render them in one loop here
			renderingMiniWorlds = true;

			if (mainCamera && miniWorld && miniWorld.clipCenter)
			{
				miniCamera.CopyFrom(mainCamera);

				miniCamera.cullingMask = cullingMask;
				miniCamera.clearFlags = CameraClearFlags.Nothing;
				miniCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * miniWorld.Matrix;
				Shader shader = Shader.Find("Custom/Custom Clip Planes");
				//mainCamera.SetReplacementShader(shader, null);
				if (miniWorld.clipCenter)
				{
					Shader.SetGlobalVector("_ClipCenter", miniWorld.clipCenter.position);
					//Shader.SetGlobalFloat("_ClipDistance", miniWorld.clipDistance);
					for (int i = 0; i < 4; i++)
						Shader.SetGlobalFloat("_ClipDistance" + i.ToString(), miniWorld.clipDistances[i]);
				}
				miniCamera.RenderWithShader(shader, string.Empty);
			}

			renderingMiniWorlds = false;
		}
	}
}