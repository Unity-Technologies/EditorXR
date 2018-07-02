#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    [ExecuteInEditMode]
    public class PlayerFloor : MonoBehaviour, IUsesViewerScale
    {
        const float k_XOffset = 0.05f;
        const float k_ZOffset = 0.2f;

        Vector3 m_FloorPosition;
        Transform m_Camera;
        Transform m_CameraRig;

        Vector3 m_CameraForwardCurrent;
        Vector3 m_CameraForwardTarget;

        void Awake()
        {
            m_Camera = CameraUtils.GetMainCamera().transform;
            m_CameraRig = CameraUtils.GetCameraRig();
        }

        void Update()
        {
            const float kLerpMultiplier = 4f;
            var currentScale = this.GetViewerScale();
            m_FloorPosition.x = m_Camera.position.x + k_XOffset * currentScale;
            m_FloorPosition.z = m_Camera.position.z - k_ZOffset * currentScale;
            m_FloorPosition.y = m_CameraRig.transform.position.y;
            m_FloorPosition -= VRView.headCenteredOrigin * currentScale;
            transform.position = m_FloorPosition;
            m_CameraForwardTarget = m_Camera.transform.XZForward();
            m_CameraForwardCurrent = Vector3.Lerp(m_CameraForwardCurrent, m_CameraForwardTarget, Time.unscaledDeltaTime * kLerpMultiplier);
            transform.forward = m_CameraForwardCurrent;
        }
    }
}
#endif
