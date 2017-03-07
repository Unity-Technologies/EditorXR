#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SceneObjectModule : MonoBehaviour, IUsesSpatialHash
	{
		const float k_InstantiateFOVDifference = -5f;
		const float k_GrowDuration = 0.5f;

		public Func<Transform, Vector3, bool> shouldPlaceObject;

		public Action<GameObject> addToSpatialHash { private get; set; }
		public Action<GameObject> removeFromSpatialHash { private get; set; }


		public void PlaceSceneObject(Transform obj, Vector3 targetScale)
		{
			if (shouldPlaceObject == null || shouldPlaceObject(obj, targetScale))
				StartCoroutine(PlaceSceneObjectCoroutine(obj, targetScale));
		}

		public void DeleteSceneObject(GameObject sceneObject)
		{
			removeFromSpatialHash(sceneObject);
			ObjectUtils.Destroy(sceneObject);
		}

		IEnumerator PlaceSceneObjectCoroutine(Transform obj, Vector3 targetScale)
		{
			// Don't let us direct select while placing
			removeFromSpatialHash(obj.gameObject);

			float start = Time.realtimeSinceStartup;
			var currTime = 0f;

			obj.parent = null;
			var startScale = obj.localScale;
			var startPosition = obj.position;
			var startRotation = obj.rotation;
			var targetRotation = MathUtilsExt.ConstrainYawRotation(startRotation);

			//Get bounds at target scale
			var origScale = obj.localScale;
			obj.localScale = targetScale;
			var bounds = ObjectUtils.GetBounds(obj.gameObject);
			obj.localScale = origScale;

			// We want to position the object so that it fits within the camera perspective at its original scale
			var camera = CameraUtils.GetMainCamera();
			var halfAngle = camera.fieldOfView * 0.5f;
			var perspective = halfAngle + k_InstantiateFOVDifference;
			var camPosition = camera.transform.position;
			var forward = obj.position - camPosition;

			var distance = bounds.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
			var targetPosition = obj.position;
			if (distance > forward.magnitude && obj.localScale != targetScale)
				targetPosition = camPosition + forward.normalized * distance;

			while (currTime < k_GrowDuration)
			{
				currTime = Time.realtimeSinceStartup - start;
				var t = currTime / k_GrowDuration;
				var tSquared = t * t;
				obj.localScale = Vector3.Lerp(startScale, targetScale, tSquared);
				obj.position = Vector3.Lerp(startPosition, targetPosition, tSquared);
				obj.rotation = Quaternion.Lerp(startRotation, targetRotation, tSquared);
				yield return null;
			}
			obj.localScale = targetScale;
			Selection.activeGameObject = obj.gameObject;

			addToSpatialHash(obj.gameObject);
		}
	}
}
#endif
