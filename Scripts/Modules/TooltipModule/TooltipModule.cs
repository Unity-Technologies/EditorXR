#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class TooltipModule : MonoBehaviour, IUsesViewerScale
    {
        const float k_Delay = 0; // In case we want to bring back a delay
        const float k_TransitionDuration = 0.1f;
        const float k_UVScale = 100f;
        const float k_UVScrollSpeed = 1.5f;
        const float k_Offset = 0.05f;
        const float k_TextOrientationWeight = 0.1f;
        const float k_ChangeTransitionDuration = 0.1f;

        const int k_PoolInitialCapacity = 16;

        static readonly Quaternion k_FlipYRotation = Quaternion.AngleAxis(180f, Vector3.up);
        static readonly Quaternion k_FlipZRotation = Quaternion.AngleAxis(180f, Vector3.forward);

        [SerializeField]
        GameObject m_TooltipPrefab;

        [SerializeField]
        GameObject m_TooltipCanvasPrefab;

        class TooltipData
        {
            public float startTime;
            public float lastModifiedTime;
            public TooltipUI tooltipUI;
            public bool persistent;
            public float duration;
            public Action becameVisible;
            public ITooltipPlacement placement;
            public float orientationWeight;
            public Vector3 transitionOffset;
            public float transitionTime;

            public Transform GetTooltipTarget(ITooltip tooltip)
            {
                if (placement != null)
                    return placement.tooltipTarget;

                return ((MonoBehaviour)tooltip).transform;
            }
        }

        readonly Dictionary<ITooltip, TooltipData> m_Tooltips = new Dictionary<ITooltip, TooltipData>();
        readonly Queue<TooltipUI> m_TooltipPool = new Queue<TooltipUI>(k_PoolInitialCapacity);

        Transform m_TooltipCanvas;
        Vector3 m_TooltipScale;

        // Local method use only -- created here to reduce garbage collection
        static readonly List<ITooltip> k_TooltipsToRemove = new List<ITooltip>();
        static readonly List<ITooltip> k_TooltipList = new List<ITooltip>();
        static readonly List<TooltipUI> k_TooltipUIs = new List<TooltipUI>();

        void Start()
        {
            m_TooltipCanvas = Instantiate(m_TooltipCanvasPrefab).transform;
            m_TooltipCanvas.SetParent(transform);
            m_TooltipScale = m_TooltipPrefab.transform.localScale;
        }

        void Update()
        {
            k_TooltipsToRemove.Clear();
            foreach (var kvp in m_Tooltips)
            {
                var tooltip = kvp.Key;
                var tooltipData = kvp.Value;
                var hoverTime = Time.time - tooltipData.startTime;
                if (hoverTime > k_Delay)
                {
                    var placement = tooltipData.placement;
                    var target = tooltipData.GetTooltipTarget(tooltip);

                    if (target == null)
                        k_TooltipsToRemove.Add(tooltip);

                    var tooltipUI = tooltipData.tooltipUI;
                    if (!tooltipUI)
                    {
                        tooltipUI = CreateTooltipObject();
                        tooltipUI.Show(tooltip.tooltipText, placement.tooltipAlignment);
                        tooltipUI.becameVisible += tooltipData.becameVisible;
                        tooltipData.tooltipUI = tooltipUI;
                        tooltipUI.dottedLine.gameObject.SetActive(true);
                        foreach (var sphere in tooltipUI.spheres)
                        {
                            sphere.gameObject.SetActive(true);
                        }
                    }

                    var lerp = Mathf.Clamp01((hoverTime - k_Delay) / k_TransitionDuration);
                    UpdateVisuals(tooltip, tooltipData, lerp);
                }

                if (!IsValidTooltip(tooltip))
                    k_TooltipsToRemove.Add(tooltip);

                if (tooltipData.persistent)
                {
                    var duration = tooltipData.duration;
                    if (duration > 0 && Time.time - tooltipData.lastModifiedTime + k_Delay > duration)
                        k_TooltipsToRemove.Add(tooltip);
                }
            }

            foreach (var tooltip in k_TooltipsToRemove)
            {
                HideTooltip(tooltip, true);
            }
        }

        TooltipUI CreateTooltipObject()
        {
            if (m_TooltipPool.Count > 0)
            {
                var pooledTooltip = m_TooltipPool.Dequeue();
                pooledTooltip.gameObject.SetActive(true);
                return pooledTooltip;
            }

            var tooltipObject = ObjectUtils.Instantiate(m_TooltipPrefab, m_TooltipCanvas);
            tooltipObject.GetComponents(k_TooltipUIs);

            var tooltipUI = k_TooltipUIs[0]; // We expect exactly one TooltipUI on the prefab root

            return tooltipUI;
        }

        void UpdateVisuals(ITooltip tooltip, TooltipData tooltipData, float lerp)
        {
            var target = tooltipData.GetTooltipTarget(tooltip);
            var tooltipUI = tooltipData.tooltipUI;
            var placement = tooltipData.placement;
            var orientationWeight = tooltipData.orientationWeight;
            var tooltipTransform = tooltipUI.transform;

            lerp = MathUtilsExt.SmoothInOutLerpFloat(lerp); // shape the lerp for better presentation
            var transitionLerp = MathUtilsExt.SmoothInOutLerpFloat(1.0f - Mathf.Clamp01((Time.time - tooltipData.transitionTime) / k_ChangeTransitionDuration));

            var viewerScale = this.GetViewerScale();
            tooltipTransform.localScale = m_TooltipScale * lerp * viewerScale;

            // Adjust for alignment
            var offset = GetTooltipOffset(tooltipUI, placement, tooltipData.transitionOffset * transitionLerp);

            // The rectTransform expansion is handled in the Tooltip dynamically, based on alignment & text length
            var rotationOffset = Quaternion.identity;
            var camTransform = CameraUtils.GetMainCamera().transform;
            if (Vector3.Dot(camTransform.forward, target.forward) < 0)
                rotationOffset *= k_FlipYRotation;

            if (Vector3.Dot(camTransform.up, target.up) + orientationWeight < 0)
            {
                rotationOffset *= k_FlipZRotation;
                tooltipData.orientationWeight = -k_TextOrientationWeight;
            }
            else
            {
                tooltipData.orientationWeight = k_TextOrientationWeight;
            }

            MathUtilsExt.SetTransformOffset(target, tooltipTransform, offset * lerp, rotationOffset);

            if (placement != null)
            {
                var rectTransform = tooltipUI.rectTransform;
                var rect = rectTransform.rect;
                var halfWidth = rect.width * 0.5f;
                var halfHeight = rect.height * 0.5f;

                var source = placement.tooltipSource;
                var toSource = tooltipTransform.InverseTransformPoint(source.position);

                // Position spheres: one at source, one on the closest edge of the tooltip
                var spheres = tooltipUI.spheres;
                spheres[0].position = source.position;

                var attachedSphere = spheres[1];
                var boxSlope = halfHeight / halfWidth;
                var toSourceSlope = Mathf.Abs(toSource.y / toSource.x);

                halfHeight *= Mathf.Sign(toSource.y);
                halfWidth *= Mathf.Sign(toSource.x);
                attachedSphere.localPosition = toSourceSlope > boxSlope
                    ? new Vector3(0, halfHeight)
                    : new Vector3(halfWidth, 0);

                // Align dotted line
                var attachedSpherePosition = attachedSphere.position;
                toSource = source.position - attachedSpherePosition;
                var midPoint = attachedSpherePosition + toSource * 0.5f;
                var dottedLine = tooltipUI.dottedLine;
                var length = toSource.magnitude;
                var uvRect = dottedLine.uvRect;
                var worldScale = 1 / viewerScale;
                uvRect.width = length * k_UVScale * worldScale;
                uvRect.xMin += k_UVScrollSpeed * Time.deltaTime;
                dottedLine.uvRect = uvRect;

                var dottedLineTransform = dottedLine.transform.parent.GetComponent<RectTransform>();
                dottedLineTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length / tooltipTransform.lossyScale.x);
                dottedLineTransform.position = midPoint;
                dottedLineTransform.rotation = Quaternion.LookRotation(toSource, -tooltipTransform.forward);
            }
        }

        public void OnRayEntered(GameObject gameObject, RayEventData eventData)
        {
            if (gameObject == this.gameObject)
                return;

            k_TooltipList.Clear();
            gameObject.GetComponents(k_TooltipList);
            foreach (var tooltip in k_TooltipList)
            {
                ShowTooltip(tooltip);
            }
        }

        public void OnRayHovering(GameObject gameObject, RayEventData eventData)
        {
            if (gameObject == this.gameObject)
                return;

            k_TooltipList.Clear();
            gameObject.GetComponents(k_TooltipList);
            foreach (var tooltip in k_TooltipList)
            {
                ShowTooltip(tooltip);
            }
        }

        public void OnRayExited(GameObject gameObject, RayEventData eventData)
        {
            if (gameObject && gameObject != this.gameObject)
            {
                k_TooltipList.Clear();
                gameObject.GetComponents(k_TooltipList);
                foreach (var tooltip in k_TooltipList)
                {
                    HideTooltip(tooltip);
                }
            }
        }

        public void ShowTooltip(ITooltip tooltip, bool persistent = false, float duration = 0f, ITooltipPlacement placement = null, Action becameVisible = null)
        {
            if (!IsValidTooltip(tooltip))
                return;

            TooltipData data;
            if (m_Tooltips.TryGetValue(tooltip, out data))
            {
                // Compare the targets to see if they changed
                var currentTarget = data.GetTooltipTarget(tooltip);
                var currentPlacement = data.placement;

                data.persistent |= persistent;
                data.placement = placement ?? tooltip as ITooltipPlacement;

                var newTarget = data.GetTooltipTarget(tooltip);
                if (currentTarget != newTarget)
                {
                    // Get the different between the 'old' tooltip position and 'new' tooltip position, even taking alignment into account
                    var transitionLerp = 1.0f - Mathf.Clamp01((Time.time - data.transitionTime) / k_ChangeTransitionDuration);
                    var currentPosition = currentTarget.TransformPoint(GetTooltipOffset(data.tooltipUI, currentPlacement, data.transitionOffset * transitionLerp));
                    var newPosition = newTarget.TransformPoint(GetTooltipOffset(data.tooltipUI, data.placement, Vector3.zero));

                    // Store it as an additional offset that we'll quickly lerp from
                    data.transitionOffset = newTarget.InverseTransformVector(currentPosition - newPosition);
                    data.transitionTime = Time.time;
                }

                if (duration > 0)
                {
                    data.duration = duration;
                    data.lastModifiedTime = Time.time;
                }

                return;
            }

            // Negative durations only affect existing tooltips
            if (duration < 0)
                return;

            m_Tooltips[tooltip] = new TooltipData
            {
                startTime = Time.time,
                lastModifiedTime = Time.time,
                persistent = persistent,
                duration = duration,
                becameVisible = becameVisible,
                placement = placement ?? tooltip as ITooltipPlacement,
                orientationWeight = 0.0f,
                transitionOffset = Vector3.zero,
                transitionTime =  0.0f,
            };
        }

        static bool IsValidTooltip(ITooltip tooltip)
        {
            return !string.IsNullOrEmpty(tooltip.tooltipText);
        }

        public void HideTooltip(ITooltip tooltip, bool persistent = false)
        {
            TooltipData tooltipData;
            if (m_Tooltips.TryGetValue(tooltip, out tooltipData))
            {
                if (!persistent && tooltipData.persistent)
                    return;

                m_Tooltips.Remove(tooltip);

                if (tooltipData.tooltipUI)
                    StartCoroutine(AnimateHide(tooltip, tooltipData));
            }
        }

        IEnumerator AnimateHide(ITooltip tooltip, TooltipData data)
        {
            var target = data.GetTooltipTarget(tooltip);
            var startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < k_TransitionDuration)
            {
                if (!target)
                    break;

                UpdateVisuals(tooltip, data, 1 - (Time.realtimeSinceStartup - startTime) / k_TransitionDuration);
                yield return null;
            }

            RecycleTooltip(data);
        }

        Vector3 GetTooltipOffset(TooltipUI tooltipUI, ITooltipPlacement placement, Vector3 transitionOffset)
        {
            if (tooltipUI == null)
            {
                return Vector3.zero;
            }

            var offset = Vector3.zero;
            if (placement != null)
            {
                switch (placement.tooltipAlignment)
                {
                    case TextAlignment.Right:
                        offset = Vector3.left;
                        break;
                    case TextAlignment.Left:
                        offset = Vector3.right;
                        break;
                }
            }

            // The rectTransform expansion is handled in the Tooltip dynamically, based on alignment & text length
            var rectTransform = tooltipUI.rectTransform;
            var rect = rectTransform.rect;
            var halfWidth = rect.width * 0.5f;

            if (placement != null)
                offset *= halfWidth * rectTransform.lossyScale.x;
            else
                offset = Vector3.back * k_Offset * this.GetViewerScale();

            offset += transitionOffset;

            return offset;
        }

        void RecycleTooltip(TooltipData tooltipData)
        {
            var tooltipUI = tooltipData.tooltipUI;
            tooltipUI.becameVisible -= tooltipData.becameVisible;
            tooltipUI.gameObject.SetActive(false);
            if (tooltipUI.removeSelf != null)
                tooltipUI.removeSelf(tooltipUI);

            m_TooltipPool.Enqueue(tooltipUI);
        }
    }
}
#endif
