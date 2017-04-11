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
		public static float DistanceToCamera(GameObject obj)
		{
			// from http://forum.unity3d.com/threads/camera-to-object-distance.32643/
			var cam = GetMainCamera();
			var distance = 0f;
			if (cam)
			{
				var heading = obj.transform.position - cam.transform.position;
				distance = Vector3.Dot(heading, cam.transform.forward);
			}
			return distance;
		}

		public static float GetSizeForDistanceToCamera(GameObject obj, float minScale, float scaleAt100)
		{
			var dist = DistanceToCamera(obj);
			var scale = MathUtilsExt.Map(dist, 0, 100, minScale, scaleAt100);
			if (scale < minScale)
				scale = minScale;

			return scale;
		}

		public static Camera GetMainCamera()
		{
			var camera = Camera.main;

#if UNITY_EDITOR && UNITY_EDITORVR
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

#if UNITY_EDITOR && UNITY_EDITORVR
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
		/// <param name="parentRotation">Glboal rotation of the parent transform</param>
		/// <returns></returns>
		public static Quaternion LocalRotateTowardCamera(Quaternion parentRotation)
		{
			var camVector = Quaternion.Inverse(parentRotation) * GetMainCamera().transform.forward;
			camVector.x = 0;
			return Quaternion.LookRotation(camVector, Vector3.Dot(camVector, Vector3.forward) > 0 ? Vector3.up : Vector3.down);
		}
	}
}
#endif
