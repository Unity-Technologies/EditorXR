using UnityEditor;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.VR.Utilities;

[RequireComponent(typeof(Camera))]
public class VRSmoothCamera : MonoBehaviour
{
	public Camera smoothCamera { get { return m_SmoothCamera; } }
	Camera m_SmoothCamera;

	[SerializeField]
	int m_TargetDisplay;
	[SerializeField, Range(1, 180)]
	int m_FieldOfView = 90;
	[SerializeField]
	float m_PositionSmoothingMultiplier = 10f;
	[SerializeField]
	float m_RotationSmoothingMultiplier = 1f;

	Camera m_VRCamera;
	RenderTexture m_RenderTexture;

	void Start()
	{
		m_VRCamera = GetComponent<Camera>();

		m_SmoothCamera = U.Object.CreateGameObjectWithComponent<Camera>();
		m_SmoothCamera.transform.position = m_VRCamera.transform.position;
		m_SmoothCamera.transform.rotation = m_VRCamera.transform.rotation;
		m_SmoothCamera.enabled = false;
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_SmoothCamera.gameObject);
	}

	void LateUpdate()
	{
		Vector3 position = m_SmoothCamera.transform.position;
		Quaternion rotation = m_SmoothCamera.transform.rotation;

		m_SmoothCamera.CopyFrom(m_VRCamera); // This copies the transform as well
		var vrCameraTexture = m_VRCamera.targetTexture;
		if (!m_RenderTexture || m_RenderTexture.width != vrCameraTexture.width || m_RenderTexture.height != vrCameraTexture.height) 
		{
			Rect guiRect = new Rect(0, 0, vrCameraTexture.width, vrCameraTexture.height);
			Rect cameraRect = EditorGUIUtility.PointsToPixels(guiRect);
			VRView.activeView.CreateCameraTargetTexture(m_RenderTexture, cameraRect, false);
		}
		m_SmoothCamera.targetTexture = m_RenderTexture;
		m_SmoothCamera.targetDisplay = m_TargetDisplay;
		m_SmoothCamera.cameraType = CameraType.Game;
		m_SmoothCamera.rect = new Rect(0, 0, 1f, 1f);
		m_SmoothCamera.stereoTargetEye = StereoTargetEyeMask.None;
		m_SmoothCamera.fieldOfView = m_FieldOfView;
		
		m_SmoothCamera.transform.position = Vector3.Lerp(position, m_VRCamera.transform.position, Time.unscaledDeltaTime* m_PositionSmoothingMultiplier);
		m_SmoothCamera.transform.rotation = Quaternion.Slerp(rotation, m_VRCamera.transform.rotation, Time.unscaledDeltaTime * m_RotationSmoothingMultiplier);

		RenderTexture.active = m_SmoothCamera.targetTexture;
		m_SmoothCamera.Render();
		RenderTexture.active = null;
	}
}
