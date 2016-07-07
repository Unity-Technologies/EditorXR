namespace UnityEditor.VR.Utilities
{
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor.VR;
#endif

    /// <summary>
    /// EditorVR Utilities
    /// </summary>
    public partial class U
    {
        /// <summary>
        /// Camera related EditorVR utilities
        /// </summary>
        public class Camera
        {
            public static float DistanceToCamera(GameObject obj)
            {
                // from http://forum.unity3d.com/threads/camera-to-object-distance.32643/
                UnityEngine.Camera cam = GetMainCamera();
                float distance = 0f;
                if (cam)
                {
                    Vector3 heading = obj.transform.position - cam.transform.position;
                    distance = Vector3.Dot(heading, cam.transform.forward);
                }
                return distance;
            }

            public static float GetSizeForDistanceToCamera(GameObject obj, float minScale, float scaleAt100)
            {
                float dist = DistanceToCamera(obj);
                float scale = MathUtilities.Map(dist, 0, 100, minScale, scaleAt100);
                if (scale < minScale) scale = minScale;
                return scale;
            }
            
            public static UnityEngine.Camera GetMainCamera()
            {
                UnityEngine.Camera camera = UnityEngine.Camera.main;
#if UNITY_EDITOR
                if (!Application.isPlaying && EditorVRView.viewerCamera)
                {
                    camera = EditorVRView.viewerCamera;
                }
#endif

                return camera;
            }

            public static Transform GetViewerPivot()
            {
                Transform pivot = UnityEngine.Camera.main ? UnityEngine.Camera.main.transform.parent : null;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (EditorVRView.viewerCamera)
                        pivot = EditorVRView.viewerCamera.transform.parent;
                }
#endif
                return pivot;
            }
        }
    }
}