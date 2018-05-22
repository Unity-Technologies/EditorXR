
#if !UNITY_2017_2_OR_NEWER
#pragma warning disable 649 // "never assigned to" warning
#endif

using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class SceneObjectModule : MonoBehaviour, ISystemModule, IUsesSpatialHash
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
            ObjectUtils.Destroy(sceneObject, withUndo: true);
        }

        IEnumerator PlaceSceneObjectCoroutine(Transform obj, Vector3 targetScale)
        {
            var go = obj.gameObject;
            // Don't let us direct select while placing
            this.RemoveFromSpatialHash(go);

            var start = Time.realtimeSinceStartup;
            var currTime = 0f;

            obj.parent = null;
            var startScale = obj.localScale;
            var startPosition = ObjectUtils.GetBounds(obj).center;
            var pivotOffset = obj.position - startPosition;
            var startRotation = obj.rotation;
            var targetRotation = MathUtilsExt.ConstrainYawRotation(startRotation);

            //Get bounds at target scale and rotation (scaled and rotated from bounds center)
            var origScale = obj.localScale;
            obj.localScale = targetScale;
            obj.rotation = targetRotation;
            var rotationDiff = Quaternion.Inverse(startRotation) * targetRotation;
            var scaleDiff = targetScale.magnitude / startScale.magnitude;
            var targetPivotOffset = rotationDiff * pivotOffset * scaleDiff;
            obj.position = startPosition + targetPivotOffset;
            var bounds = ObjectUtils.GetBounds(obj);
            obj.localScale = origScale;
            obj.localRotation = startRotation;
            obj.position = startPosition + pivotOffset;

            // We want to position the object so that it fits within the camera perspective at its original scale
            var camera = CameraUtils.GetMainCamera();
            var halfAngle = camera.fieldOfView * 0.5f;
            var perspective = halfAngle + k_InstantiateFOVDifference;
            var camPosition = camera.transform.position;
            var forward = startPosition - camPosition;

            var distance = bounds.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
            var targetPosition = bounds.center;
            if (distance > forward.magnitude && obj.localScale != targetScale)
                targetPosition = camPosition + forward.normalized * distance;

            startPosition += pivotOffset;
            targetPosition += targetPivotOffset;

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
            Selection.activeGameObject = go;

            this.AddToSpatialHash(go);
#if UNITY_EDITOR
            Undo.IncrementCurrentGroup();
#endif
        }

        public void PlaceSceneObjects(Transform[] transforms, Vector3[] targetPositionOffsets, Quaternion[] targetRotations, Vector3[] targetScales)
        {
            StartCoroutine(PlaceSceneObjectsCoroutine(transforms, targetPositionOffsets, targetRotations, targetScales));
        }

        IEnumerator PlaceSceneObjectsCoroutine(Transform[] transforms, Vector3[] targetPositionOffsets, Quaternion[] targetRotations, Vector3[] targetScales)
        {
            var start = Time.realtimeSinceStartup;
            var currTime = 0f;

            var length = transforms.Length;
            var startPositions = new Vector3[length];
            var startRotations = new Quaternion[length];
            var startScales = new Vector3[length];
            var center = ObjectUtils.GetBounds(transforms).center;
            var pivot = Vector3.zero;

            //Get bounds at target scale and rotation (scaled and rotated from bounds center)
            for (var i = 0; i < length; i++)
            {
                var transform = transforms[i];
                this.RemoveFromSpatialHash(transform.gameObject);
                var position = transform.position;
                startPositions[i] = position;
                startRotations[i] = transform.rotation;
                startScales[i] = transform.localScale;

                pivot += position;

                transform.position = targetPositionOffsets[i];
                transform.rotation = targetRotations[i];
                transform.localScale = targetScales[i];
            }
            pivot /= length;

            var bounds = ObjectUtils.GetBounds(transforms);

            for (var i = 0; i < length; i++)
            {
                var transform = transforms[i];
                transform.position = startPositions[i];
                transform.rotation = startRotations[i];
                transform.localScale = startScales[i];
            }

            // We want to position the object so that it fits within the camera perspective at its original scale
            var camera = CameraUtils.GetMainCamera();
            var halfAngle = camera.fieldOfView * 0.5f;
            var perspective = halfAngle + k_InstantiateFOVDifference;
            var camPosition = camera.transform.position;
            var forward = center - camPosition;

            var distance = bounds.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
            var targetPosition = pivot;
            if (distance > forward.magnitude)
                targetPosition = camPosition + forward.normalized * distance;

            for (var i = 0; i < length; i++)
            {
                targetPositionOffsets[i] += targetPosition;
            }

            while (currTime < k_GrowDuration)
            {
                currTime = Time.realtimeSinceStartup - start;
                var t = currTime / k_GrowDuration;
                var tSquared = t * t;
                for (int i = 0; i < length; i++)
                {
                    var transform = transforms[i];
                    transform.localScale = Vector3.Lerp(startScales[i], targetScales[i], tSquared);
                    transform.position = Vector3.Lerp(startPositions[i], targetPositionOffsets[i], tSquared);
                    transform.rotation = Quaternion.Slerp(startRotations[i], targetRotations[i], tSquared);
                    yield return null;
                }
            }

            var objects = new GameObject[length];
            for (int i = 0; i < length; i++)
            {
                var transform = transforms[i];
                objects[i] = transform.gameObject;
                transform.localScale = targetScales[i];
                transform.rotation = targetRotations[i];
                transform.position = targetPositionOffsets[i];

                this.AddToSpatialHash(transform.gameObject);
            }

            Selection.objects = objects;
#if UNITY_EDITOR
            Undo.IncrementCurrentGroup();
#endif
        }
    }
}

