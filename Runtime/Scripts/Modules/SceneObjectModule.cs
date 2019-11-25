using System.Collections;
using Unity.Labs.EditorXR.Core;
using Unity.Labs.EditorXR.Extensions;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.Modules
{
    sealed class SceneObjectModule : IModule, IProvidesPlaceSceneObject,
        IProvidesPlaceSceneObjects, IProvidesDeleteSceneObject, IUsesSpatialHash
    {
        const float k_InstantiateFOVDifference = -5f;
        const float k_GrowDuration = 0.5f;

        EditorXRMiniWorldModule m_MiniWorldModule;

#if !FI_AUTOFILL
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
#endif

        public void PlaceSceneObject(Transform obj, Vector3 targetScale)
        {
            if (!TryPlaceObjectInMiniWorld(obj, targetScale))
                EditorMonoBehaviour.instance.StartCoroutine(PlaceSceneObjectCoroutine(obj, targetScale));
        }

        public void DeleteSceneObject(GameObject sceneObject)
        {
            this.RemoveFromSpatialHash(sceneObject);
            UnityObjectUtils.Destroy(sceneObject, withUndo: true);
        }

        IEnumerator PlaceSceneObjectCoroutine(Transform obj, Vector3 targetScale)
        {
            var go = obj.gameObject;

            // Don't let us direct select while placing
            this.RemoveFromSpatialHash(go);

            var start = Time.realtimeSinceStartup;
            var currTime = 0f;

            obj.parent = null;
            var localScale = obj.localScale;
            var startScale = localScale;
            var startPosition = BoundsUtils.GetBounds(obj).center;
            var position = obj.position;
            var pivotOffset = position - startPosition;
            var startRotation = obj.rotation;
            var targetRotation = startRotation.ConstrainYaw();

            //Get bounds at target scale and rotation (scaled and rotated from bounds center)
            var origScale = localScale;
            obj.rotation = targetRotation;
            var rotationDiff = Quaternion.Inverse(startRotation) * targetRotation;
            var scaleDiff = targetScale.magnitude / startScale.magnitude;
            var targetPivotOffset = rotationDiff * pivotOffset * scaleDiff;
            var bounds = BoundsUtils.GetBounds(obj);
            localScale = origScale;
            obj.localScale = localScale;
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
            UnityEditor.Undo.IncrementCurrentGroup();
#endif
        }

        public void PlaceSceneObjects(Transform[] transforms, Vector3[] targetPositionOffsets, Quaternion[] targetRotations, Vector3[] targetScales)
        {
            EditorMonoBehaviour.instance.StartCoroutine(PlaceSceneObjectsCoroutine(transforms, targetPositionOffsets, targetRotations, targetScales));
        }

        IEnumerator PlaceSceneObjectsCoroutine(Transform[] transforms, Vector3[] targetPositionOffsets, Quaternion[] targetRotations, Vector3[] targetScales)
        {
            var start = Time.realtimeSinceStartup;
            var currTime = 0f;

            var length = transforms.Length;
            var startPositions = new Vector3[length];
            var startRotations = new Quaternion[length];
            var startScales = new Vector3[length];
            var center = BoundsUtils.GetBounds(transforms).center;
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

            var bounds = BoundsUtils.GetBounds(transforms);

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
            UnityEditor.Undo.IncrementCurrentGroup();
#endif
        }

        public void LoadModule() { m_MiniWorldModule = ModuleLoaderCore.instance.GetModule<EditorXRMiniWorldModule>(); }

        bool TryPlaceObjectInMiniWorld(Transform obj, Vector3 targetScale)
        {
            if (m_MiniWorldModule == null)
                return false;

            foreach (var miniWorld in m_MiniWorldModule.worlds)
            {
                if (!miniWorld.Contains(obj.position))
                    continue;

                var referenceTransform = miniWorld.referenceTransform;
                obj.transform.parent = null;
                obj.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(obj.position), miniWorld.referenceTransform.localScale);
                obj.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * obj.rotation;
                obj.localScale = Vector3.Scale(Vector3.Scale(obj.localScale, referenceTransform.localScale), miniWorld.miniWorldTransform.lossyScale.Inverse());

                this.AddToSpatialHash(obj.gameObject);
                return true;
            }

            return false;
        }

        public void UnloadModule() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var placeSceneObjectSubscriber = obj as IFunctionalitySubscriber<IProvidesPlaceSceneObject>;
            if (placeSceneObjectSubscriber != null)
                placeSceneObjectSubscriber.provider = this;

            var placeSceneObjectsSubscriber = obj as IFunctionalitySubscriber<IProvidesPlaceSceneObjects>;
            if (placeSceneObjectsSubscriber != null)
                placeSceneObjectsSubscriber.provider = this;

            var deleteObjectSubscriber = obj as IFunctionalitySubscriber<IProvidesDeleteSceneObject>;
            if (deleteObjectSubscriber != null)
                deleteObjectSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
