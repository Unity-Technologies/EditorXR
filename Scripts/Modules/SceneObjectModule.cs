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

		public Func<Transform, Vector3, bool> tryPlaceObject;

		public void PlaceSceneObject(Transform obj, Vector3 targetScale)
		{
			if (tryPlaceObject == null || !tryPlaceObject(obj, targetScale))
				StartCoroutine(PlaceSceneObjectCoroutine(obj, targetScale));
		}

		public void DeleteSceneObject(GameObject sceneObject)
		{
			this.RemoveFromSpatialHash(sceneObject);
			ObjectUtils.Destroy(sceneObject);
		}

		IEnumerator PlaceSceneObjectCoroutine(Transform obj, Vector3 targetScale)
		{
			// Don't let us direct select while placing
			this.RemoveFromSpatialHash(obj.gameObject);

			float start = Time.realtimeSinceStartup;
			var currTime = 0f;

			obj.parent = null;
			var startScale = obj.localScale;
			var startPosition = obj.position;
			var startRotation = obj.rotation;
			var targetRotation = MathUtilsExt.ConstrainYawRotation(startRotation);

			//Get bounds at target scale and rotation
			var origScale = obj.localScale;
			obj.localScale = targetScale;
			obj.rotation = targetRotation;
			var bounds = ObjectUtils.GetBounds(obj);
			obj.localScale = origScale;
			obj.localRotation = startRotation;

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

			this.AddToSpatialHash(obj.gameObject);
		}

		public void PlaceSceneObjects(Transform[] transforms, Transform parent = null, Quaternion rotationOffset = default(Quaternion), float scaleFactor = 1)
		{
			StartCoroutine(PlaceSceneObjectsCoroutine(transforms, parent, rotationOffset, scaleFactor));
		}

		IEnumerator PlaceSceneObjectsCoroutine(Transform[] transforms, Transform parent, Quaternion rotationOffset, float scaleFactor)
		{
			float start = Time.realtimeSinceStartup;
			var currTime = 0f;

			var length = transforms.Length;
			Vector3 parentStartPosition;
			Quaternion parentStartRotation;
			var rotationOffsets = new Quaternion[length];
			var positionOffsets = new Vector3[length];
			if (parent)
			{
				parentStartPosition = parent.position;
				parentStartRotation = parent.rotation;

				for (int i = 0; i < length; i++)
				{
					MathUtilsExt.GetTransformOffset(parent, transforms[i], out positionOffsets[i], out rotationOffsets[i]);
				}
			}
			else
			{
				parentStartRotation = Quaternion.identity;
				parentStartPosition = Vector3.zero;
				foreach (var transform in transforms)
				{
					parentStartPosition += transform.position;
				}
				parentStartPosition /= length;

				for (int i = 0; i < length; i++)
				{
					positionOffsets[i] = transforms[i].position - parentStartPosition;
					rotationOffsets[i] = Quaternion.identity;
				}
			}

			var targetRotation = parentStartRotation * rotationOffset;
			var startScales = new Vector3[length];
			var targetScales = new Vector3[length];

			//Get bounds at target scale and rotation
			for (int i = 0; i < length; i++)
			{
				var transform = transforms[i];
				startScales[i] = transform.localScale;
				transform.localScale *= scaleFactor;
				targetScales[i] = transform.localScale;
				transform.rotation = targetRotation * rotationOffsets[i];
				transform.position = parentStartPosition + targetRotation * positionOffsets[i] * scaleFactor;
			}
			var bounds = ObjectUtils.GetBounds(transforms);
			for (int i = 0; i < length; i++)
			{
				var transform = transforms[i];
				startScales[i] = transform.localScale;
				transform.localScale = startScales[i];
				transform.rotation = parentStartRotation * rotationOffsets[i];
				transform.position = parentStartPosition + positionOffsets[i];
			}

			// We want to position the object so that it fits within the camera perspective at its original scale
			var camera = CameraUtils.GetMainCamera();
			var halfAngle = camera.fieldOfView * 0.5f;
			var perspective = halfAngle + k_InstantiateFOVDifference;
			var camPosition = camera.transform.position;
			var forward = parentStartPosition - camPosition;

			var distance = bounds.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
			var targetPosition = parentStartPosition;
			if (distance > forward.magnitude && scaleFactor != 1)
				targetPosition = camPosition + forward.normalized * distance;

			while (currTime < k_GrowDuration)
			{
				currTime = Time.realtimeSinceStartup - start;
				var t = currTime / k_GrowDuration;
				var tSquared = t * t;
				var parentPosition = Vector3.Lerp(parentStartPosition, targetPosition, tSquared);
				var parentRotation = Quaternion.Lerp(parentStartRotation, targetRotation, tSquared);
				for (int i = 0; i < length; i++)
				{
					var obj = transforms[i];

					// Don't let us direct select while placing
					this.RemoveFromSpatialHash(obj.gameObject);
					obj.localScale = Vector3.Lerp(startScales[i], targetScales[i], tSquared);
					obj.position = parentPosition + targetRotation * positionOffsets[i] * scaleFactor;
					obj.rotation = parentRotation * rotationOffsets[i];
					yield return null;

					this.AddToSpatialHash(obj.gameObject);
				}
			}

			var objects = new GameObject[length];
			for (int i = 0; i < length; i++)
			{
				var transform = transforms[i];
				objects[i] = transform.gameObject;
				transform.localScale = targetScales[i];
			}

			Selection.objects = objects;
		}
	}
}
#endif
