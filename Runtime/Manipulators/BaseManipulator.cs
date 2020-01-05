using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Handles;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.EditorXR.Manipulators
{
    class BaseManipulator : MonoBehaviour, IManipulator
    {
        class HandleTip
        {
            public Renderer renderer;
            public float direction = 1;
            public Vector3 lastPosition;
            public Vector3? positionOffset;
        }

        const float k_MinHandleTipDirectionDelta = 0.01f;
        const float k_LazyFollow = 40f;

        protected const float k_BaseManipulatorSize = 0.3f;

#pragma warning disable 649
        [SerializeField]
        Renderer m_HandleTip;

        [SerializeField]
        protected List<BaseHandle> m_AllHandles;

        [SerializeField]
        float m_LinearHandleScaleBump = 1.8f;

        [SerializeField]
        float m_PlaneHandleScaleBump = 1.2f;

        [SerializeField]
        float m_SphereHandleScaleBump = 1.1f;

        [SerializeField]
        float m_HandleAlpha = 0.4f;

        [SerializeField]
        float m_HandleHoverAlpha = 0.8f;
#pragma warning restore 649

        readonly Dictionary<Type, float> m_ScaleBumps = new Dictionary<Type, float>();
        readonly Dictionary<Transform, HandleTip> m_HandleTips = new Dictionary<Transform, HandleTip>();

        public bool adjustScaleForCamera { get; set; }

        public Action<Vector3, Transform, AxisFlags> translate { protected get; set; }
        public Action<Quaternion, Transform> rotate { protected get; set; }
        public Action<Vector3> scale { protected get; set; }

        public bool dragging { get; protected set; }
        public event Action dragStarted;
        public event Action<Transform> dragEnded;

        void Awake()
        {
            m_ScaleBumps[typeof(LinearHandle)] = m_LinearHandleScaleBump;
            m_ScaleBumps[typeof(PlaneHandle)] = m_PlaneHandleScaleBump;
            m_ScaleBumps[typeof(SphereHandle)] = m_SphereHandleScaleBump;

            foreach (var handle in m_AllHandles)
            {
                MaterialUtils.CloneMaterials(handle.GetComponent<Renderer>());

                handle.hoverStarted += OnHandleHoverStarted;
                handle.hovering += OnHandleHovering;
                handle.hoverEnded += OnHandleHoverEnded;

                handle.dragStarted += OnHandleDragStarted;
                handle.dragging += OnHandleDragging;
                handle.dragEnded += OnHandleDragEnded;
            }
        }

        protected virtual void OnEnable()
        {
            if (adjustScaleForCamera)
                Camera.onPreRender += OnCameraPreRender;
        }

        protected virtual void OnDisable()
        {
            Camera.onPreRender -= OnCameraPreRender;

            foreach (var kvp in m_HandleTips)
            {
                kvp.Value.renderer.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            foreach (var handle in m_AllHandles)
            {
                UnityObjectUtils.Destroy(handle.GetComponent<Renderer>().sharedMaterial);
            }
        }

        protected virtual void ShowHoverState(BaseHandle handle, bool hovering)
        {
            var type = handle.GetType();
            float scaleBump;
            if (m_ScaleBumps.TryGetValue(type, out scaleBump))
                handle.transform.localScale = hovering ? handle.transform.localScale * scaleBump : handle.transform.localScale / scaleBump;

            var handleRenderer = handle.GetComponent<Renderer>();
            var material = handleRenderer.sharedMaterial;
            var color = material.color;
            color.a = hovering ? m_HandleHoverAlpha : m_HandleAlpha;
            material.color = color;
        }

        void OnCameraPreRender(Camera camera)
        {
            AdjustScale(camera.transform.position, camera.worldToCameraMatrix);
        }

        public void AdjustScale(Vector3 cameraPosition, Matrix4x4 worldToCameraMatrix)
        {
            var originalCameraPosition = cameraPosition;

            // Adjust size of manipulator while accounting for any non-standard cameras (e.g. scaling applied to the camera)
            var manipulatorPosition = worldToCameraMatrix.MultiplyPoint3x4(transform.position);
            cameraPosition = worldToCameraMatrix.MultiplyPoint3x4(cameraPosition);
            var delta = worldToCameraMatrix.inverse.MultiplyPoint3x4(cameraPosition - manipulatorPosition) - originalCameraPosition;
            transform.localScale = delta.magnitude * k_BaseManipulatorSize * Vector3.one;
        }

        protected virtual void UpdateHandleTip(BaseHandle handle, HandleEventData eventData, bool active)
        {
            var rayOrigin = eventData.rayOrigin;
            HandleTip handleTip;
            Renderer handleTipRenderer;
            if (!m_HandleTips.TryGetValue(rayOrigin, out handleTip))
            {
                handleTipRenderer = m_HandleTip;
                if (m_HandleTips.Count > 0)
                    handleTipRenderer = EditorXRUtils.Instantiate(handleTipRenderer.gameObject, transform).GetComponent<Renderer>();

                handleTip = new HandleTip { renderer = handleTipRenderer };
                m_HandleTips[rayOrigin] = handleTip;
            }
            else
            {
                handleTipRenderer = handleTip.renderer;
            }

            active = active && (handle is LinearHandle || handle is RadialHandle);

            var handleTipGameObject = handleTipRenderer.gameObject;
            var wasActive = handleTipGameObject.activeSelf;
            handleTipGameObject.SetActive(active);
            var handleTipTransform = handleTipRenderer.transform;

            if (active) // Reposition handle tip based on current raycast position when hovering or dragging
            {
                handleTipRenderer.sharedMaterial = handle.GetComponent<Renderer>().sharedMaterial;

                var handleTransform = handle.transform;
                var handleTipPosition = handleTipTransform.position;
                var distanceFromRayOrigin = Vector3.Distance(handleTipPosition, rayOrigin.position);

                var linearEventData = eventData as LinearHandle.LinearHandleEventData;
                var lerp = wasActive ? k_LazyFollow * Time.deltaTime : 1;
                if (linearEventData != null)
                {
                    handleTipTransform.position = Vector3.Lerp(handleTipPosition,
                        handleTransform.TransformPoint(new Vector3(0, 0,
                            handleTransform.InverseTransformPoint(linearEventData.raycastHitWorldPosition).z)),
                        lerp);

                    var handleForward = handleTransform.forward;
                    var delta = handleTipPosition - handleTip.lastPosition;
                    if (delta.magnitude > k_MinHandleTipDirectionDelta * distanceFromRayOrigin)
                    {
                        handleTip.direction = Mathf.Sign(Vector3.Dot(delta, handleForward));
                        handleTip.lastPosition = handleTipPosition;
                    }

                    handleTipTransform.forward = handleForward * handleTip.direction;
                }

                var radialEventData = eventData as RadialHandle.RadialHandleEventData;
                if (radialEventData != null)
                {
                    var positionOffset = handleTip.positionOffset;
                    if (positionOffset.HasValue)
                    {
                        handleTipTransform.position = handleTransform.TransformPoint(positionOffset.Value);
                    }
                    else
                    {
                        var newLocalPos = handleTransform.InverseTransformPoint(radialEventData.raycastHitWorldPosition);
                        newLocalPos.y = 0;
                        handleTipTransform.position = Vector3.Lerp(handleTipPosition,
                            handleTransform.TransformPoint(newLocalPos.normalized * 0.5f * handleTransform.localScale.x),
                            lerp);
                    }

                    var forward = Vector3.Cross(handleTransform.up, (handleTipPosition - handleTransform.position).normalized);
                    var delta = handleTipPosition - handleTip.lastPosition;
                    if (delta.magnitude > k_MinHandleTipDirectionDelta * distanceFromRayOrigin)
                    {
                        handleTip.direction = Mathf.Sign(Vector3.Dot(delta, forward));
                        handleTip.lastPosition = handleTipPosition;
                    }

                    if (forward != Vector3.zero)
                        handleTipTransform.forward = forward * handleTip.direction;
                }

                if (handle.hasDragSource && !handleTip.positionOffset.HasValue)
                    handleTip.positionOffset = handle.transform.InverseTransformPoint(handleTipTransform.position);
            }
            else if (!handle.hasDragSource)
            {
                handleTip.positionOffset = null;
            }
        }

        protected virtual void OnHandleHoverStarted(BaseHandle handle, HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;
            if (handle.IndexOfHoverSource(rayOrigin) > 0)
                return;

            if (!handle.hasDragSource)
            {
                ShowHoverState(handle, true);
                UpdateHandleTip(handle, eventData, true);
            }
        }

        protected virtual void OnHandleHovering(BaseHandle handle, HandleEventData eventData)
        {
            if (handle.IndexOfHoverSource(eventData.rayOrigin) > 0)
                return;

            if (!handle.hasDragSource)
                UpdateHandleTip(handle, eventData, true);
        }

        protected virtual void OnHandleHoverEnded(BaseHandle handle, HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;
            if (handle.IndexOfHoverSource(rayOrigin) > 0)
                return;

            if (!handle.hasDragSource)
            {
                UpdateHandleTip(handle, eventData, false);

                if (!handle.hasHoverSource)
                    ShowHoverState(handle, false);
            }
        }

        protected virtual void OnHandleDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;
            if (handle.IndexOfDragSource(rayOrigin) > 0)
                return;

            foreach (var h in m_AllHandles)
                h.gameObject.SetActive(h == handle);

            foreach (var kvp in m_HandleTips)
                kvp.Value.renderer.gameObject.SetActive(false);

            if (dragStarted != null)
                dragStarted();

            dragging = true;

            ShowHoverState(handle, false);
            UpdateHandleTip(handle, eventData, true);
        }

        protected virtual void OnHandleDragging(BaseHandle handle, HandleEventData eventData)
        {
            if (handle.IndexOfDragSource(eventData.rayOrigin) != 0)
                return;

            UpdateHandleTip(handle, eventData, true);
        }

        protected virtual void OnHandleDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;

            UpdateHandleTip(handle, eventData, false);

            if (handle.hasDragSource)
                return;

            foreach (var h in m_AllHandles)
                h.gameObject.SetActive(true);

            if (dragEnded != null)
                dragEnded(rayOrigin);

            dragging = false;

            if (!handle.hasDragSource && handle.hasHoverSource)
                ShowHoverState(handle, true);
        }
    }
}
