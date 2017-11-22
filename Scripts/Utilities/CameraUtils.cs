#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    /// <summary>
    /// Camera related EditorVR utilities
    /// </summary>
    static class CameraUtils
    {
        public static Camera GetMainCamera()
        {
            var camera = Camera.main;

#if UNITY_EDITOR
            if (!Application.isPlaying && VRView.viewerCamera)
            {
                camera = VRView.viewerCamera;
            }
#endif

            return camera;
        }

        public static Transform GetCameraRig()
        {
            var rig = Camera.main ? Camera.main.transform.parent : null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (VRView.cameraRig)
                    rig = VRView.cameraRig;
            }
#endif
            return rig;
        }

        /// <summary>
        /// Returns a local roll-only rotation which will face the object toward the camera
        /// </summary>
        /// <param name="parentTransform">The parent transform</param>
        /// <returns></returns>
        public static Quaternion LocalRotateTowardCamera(Transform parentTransform)
        {
            var camToParent = parentTransform.position - GetMainCamera().transform.position;
            var camVector = Quaternion.Inverse(parentTransform.rotation) * camToParent;
            camVector.x = 0;

            return Quaternion.LookRotation(camVector,
                Vector3.Dot(camVector, Vector3.forward) > 0 ? Vector3.up : Vector3.down);
        }
    }
}
#endif
