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
        Vector3 m_floorPosition;
        Transform m_Camera;
        Transform m_CameraRig;

        void Awake()
        {
            m_Camera = CameraUtils.GetMainCamera().transform;
            m_CameraRig = CameraUtils.GetCameraRig();
        }

        const float xOffset = 0.05f;
        const float zOffset = 0.2f;
        var currentScale = this.GetViewerScale();
        void Update()
        {
            m_floorPosition.x = m_Camera.position.x + xOffset * currentScale;
            m_floorPosition.z = m_Camera.position.z - zOffset * currentScale;
            m_floorPosition.y = m_CameraRig.transform.position.y;
            m_floorPosition -= VRView.headCenteredOrigin * currentScale;
            transform.position = m_floorPosition;
            transform.forward = m_Camera.transform.XZForward();
        }
    }

}
#endif