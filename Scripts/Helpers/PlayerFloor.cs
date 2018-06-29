#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    [ExecuteInEditMode]
    public class PlayerFloor : MonoBehaviour
    {
        Vector3 floorPosition;
        Quaternion floorRotation;
        Transform m_Camera;
        Transform m_CameraRig;

        private void Awake()
        {
            m_Camera = CameraUtils.GetMainCamera().transform;
            m_CameraRig = CameraUtils.GetCameraRig();
        }

        void Update()
        {
            floorPosition.x = m_Camera.position.x;
            floorPosition.z = m_Camera.position.z - 0.2f;
            floorPosition.y = m_CameraRig.transform.position.y;
            floorPosition -= VRView.headCenteredOrigin;
            transform.position = floorPosition;
            transform.forward = m_Camera.transform.XZForward();
        }
        
    }
}
#endif