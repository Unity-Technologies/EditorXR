#if !UNITY_EDITORVR
#pragma warning disable 414, 649
#endif
using UnityEditor;
using UnityEditor.Experimental.EditorVR;

namespace UnityEngine.Experimental.EditorVR.Helpers
{
	/// <summary>
	/// A preview camera that provides for smoothing of the position and look vector
	/// </summary>
	[RequireComponent(typeof(Camera))]
	[RequiresLayer(kHMDOnlyLayer)]
	public class VRSmoothCamera : MonoBehaviour, IPreviewCamera
	{
		/// <summary>
		/// The camera drawing the preview
		/// </summary>
		public Camera previewCamera { get { return m_SmoothCamera; } }
		Camera m_SmoothCamera;

		/// <summary>
		/// The actual HMD camera (will be provided by system)
		/// The VRView's camera, whose settings are copied by the SmoothCamera
		/// </summary>
		public Camera vrCamera { private get { return m_VRCamera; } set { m_VRCamera = value; } }
		[SerializeField]
		Camera m_VRCamera;

		[SerializeField]
		int m_TargetDisplay;
		[SerializeField, Range(1, 180)]
		int m_FieldOfView = 40;
		[SerializeField]
		float m_SmoothingMultiplier = 3;

		const string kHMDOnlyLayer = "HMDOnly";

		RenderTexture m_RenderTexture;

		Vector3 m_Position;
		Vector3 m_Forward;

		/// <summary>
		/// A layer mask that controls what will always render in the HMD and not in the preview
		/// </summary>
		public int hmdOnlyLayerMask { get { return LayerMask.GetMask(kHMDOnlyLayer); } }

#if UNITY_EDITORVR
		void Awake()
		{
			m_SmoothCamera = GetComponent<Camera>();
			m_SmoothCamera.enabled = false;
		}

		void Start()
		{
			transform.position = m_VRCamera.transform.position;
			transform.rotation = m_VRCamera.transform.rotation;

			m_Position = transform.position;
			m_Forward = transform.forward;
		}

		void LateUpdate()
		{
			m_SmoothCamera.CopyFrom(m_VRCamera); // This copies the transform as well
			var vrCameraTexture = m_VRCamera.targetTexture;
			if (vrCameraTexture && (!m_RenderTexture || m_RenderTexture.width != vrCameraTexture.width || m_RenderTexture.height != vrCameraTexture.height))
			{
				Rect guiRect = new Rect(0f, 0f, vrCameraTexture.width, vrCameraTexture.height);
				Rect cameraRect = EditorGUIUtility.PointsToPixels(guiRect);
				VRView.activeView.CreateCameraTargetTexture(ref m_RenderTexture, cameraRect, false);
				m_RenderTexture.name = "Smooth Camera RT";
			}
			m_SmoothCamera.targetTexture = m_RenderTexture;
			m_SmoothCamera.targetDisplay = m_TargetDisplay;
			m_SmoothCamera.cameraType = CameraType.Game;
			m_SmoothCamera.cullingMask &= ~hmdOnlyLayerMask;
			m_SmoothCamera.rect = new Rect(0f, 0f, 1f, 1f);
			m_SmoothCamera.stereoTargetEye = StereoTargetEyeMask.None;
			m_SmoothCamera.fieldOfView = m_FieldOfView;

			m_Position = Vector3.Lerp(m_Position, m_VRCamera.transform.position, Time.unscaledDeltaTime * m_SmoothingMultiplier);
			m_Forward = Vector3.Lerp(m_Forward, m_VRCamera.transform.forward, Time.unscaledDeltaTime * m_SmoothingMultiplier);

			const float kPullBackDistance = 1.1f;
			transform.forward = m_Forward;
			transform.position = m_Position - transform.forward * kPullBackDistance;

			// Don't render any HMD-related visual proxies
			var hidden = m_VRCamera.GetComponentsInChildren<Renderer>();
			bool[] hiddenEnabled = new bool[hidden.Length];
			for (int i = 0; i < hidden.Length; i++)
			{
				var h = hidden[i];
				hiddenEnabled[i] = h.enabled;
				h.enabled = false;
			}

			RenderTexture.active = m_SmoothCamera.targetTexture;
			m_SmoothCamera.Render();
			RenderTexture.active = null;

			for (int i = 0; i < hidden.Length; i++)
			{
				hidden[i].enabled = hiddenEnabled[i];
			}
		}
#endif
	}
}
