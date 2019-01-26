using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;
using UnityObject = UnityEngine.Object;

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

            if (!camera)
                camera = UnityObject.FindObjectOfType<Camera>();

#if UNITY_EDITOR
            var viewerCamera = VRView.viewerCamera;
            if (!Application.isPlaying && viewerCamera)
                camera = viewerCamera;
#endif

            return camera;
        }

        public static Transform GetCameraRig()
        {
            var camera = GetMainCamera();
            if (camera)
            {
                var rig = camera.transform.parent;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (VRView.cameraRig)
                        rig = VRView.cameraRig;
                }
#endif

                return rig;
            }

            return null;
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
