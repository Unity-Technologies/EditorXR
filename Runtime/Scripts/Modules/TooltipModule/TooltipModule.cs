using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class TooltipModule : ScriptableSettings<TooltipModule>, IInitializableModule, IModuleBehaviorCallbacks,
        IModuleDependency<MultipleRayInputModule>, IUsesViewerScale, IProvidesSetTooltipVisibility
    {
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

            public void Reset()
            {
                startTime = default(float);
                lastModifiedTime = default(float);
                tooltipUI = default(TooltipUI);
                persistent = default(bool);
                duration = default(float);
                becameVisible = default(Action);
                placement = default(ITooltipPlacement);
                orientationWeight = default(float);
                transitionOffset = default(Vector3);
                transitionTime = default(float);
            }
        }

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

        static readonly Vector3[] k_Corners = new Vector3[4];

#pragma warning disable 649
        [SerializeField]
        GameObject m_TooltipPrefab;

        [SerializeField]
        GameObject m_TooltipCanvasPrefab;
#pragma warning restore 649

        readonly Dictionary<ITooltip, TooltipData> m_Tooltips = new Dictionary<ITooltip, TooltipData>();
        readonly Queue<TooltipUI> m_TooltipPool = new Queue<TooltipUI>(k_PoolInitialCapacity);
        readonly Queue<TooltipData> m_TooltipDataPool = new Queue<TooltipData>(k_PoolInitialCapacity);

        Transform m_TooltipCanvas;
        Vector3 m_TooltipScale;
        GameObject m_ModuleParent;

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection
        static readonly List<ITooltip> k_TooltipsToRemove = new List<ITooltip>();
        static readonly List<ITooltip> k_TooltipList = new List<ITooltip>();
        static readonly List<TooltipUI> k_TooltipUIs = new List<TooltipUI>();

        public void ConnectDependency(MultipleRayInputModule dependency)
        {
            dependency.rayEntered += OnRayEntered;
            dependency.rayHovering += OnRayHovering;
            dependency.rayExited += OnRayExited;
        }

        public void LoadModule()
        {
            m_TooltipScale = m_TooltipPrefab.transform.localScale;
        }

        public void UnloadModule() { }

        public void Initialize()
        {
            m_ModuleParent = ModuleLoaderCore.instance.GetModuleParent();
            m_TooltipCanvas = EditorXRUtils.Instantiate(m_TooltipCanvasPrefab).transform;
            m_TooltipCanvas.SetParent(m_ModuleParent.transform);

            m_Tooltips.Clear();
            m_TooltipPool.Clear();
            m_TooltipDataPool.Clear();
        }

        public void Shutdown()
        {
            if (m_TooltipCanvas)
                UnityObjectUtils.Destroy(m_TooltipCanvas.gameObject);
        }

        public void OnBehaviorUpdate()
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

            var tooltipObject = EditorXRUtils.Instantiate(m_TooltipPrefab, m_TooltipCanvas);
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
            tooltipTransform.localScale = lerp * viewerScale * m_TooltipScale;

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
                //TODO: Figure out why rect gives us different height/width than GetWorldCorners
                tooltipUI.rectTransform.GetWorldCorners(k_Corners);
                var bottomLeft = k_Corners[0];
                var halfWidth = (bottomLeft - k_Corners[2]).magnitude * 0.5f;
                var halfHeight = (bottomLeft - k_Corners[1]).magnitude * 0.5f;

                var source = placement.tooltipSource;
                var sourcePosition = source.position;
                var toSource = tooltipTransform.InverseTransformPoint(sourcePosition);

                // Position spheres: one at source, one on the closest edge of the tooltip
                var spheres = tooltipUI.spheres;
                spheres[0].position = sourcePosition;

                var attachedSphere = spheres[1];
                var boxSlope = halfHeight / halfWidth;
                var toSourceSlope = Mathf.Abs(toSource.y / toSource.x);

                var parentScale = attachedSphere.parent.lossyScale;
                halfHeight *= Mathf.Sign(toSource.y) / parentScale.x;
                halfWidth *= Mathf.Sign(toSource.x) / parentScale.y;
                attachedSphere.localPosition = toSourceSlope > boxSlope
                    ? new Vector3(0, halfHeight)
                    : new Vector3(halfWidth, 0);

                // Align dotted line
                var attachedSpherePosition = attachedSphere.position;
                toSource = sourcePosition - attachedSpherePosition;
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

        void OnRayEntered(GameObject gameObject, RayEventData eventData)
        {
            if (gameObject == m_ModuleParent)
                return;

            k_TooltipList.Clear();
            gameObject.GetComponents(k_TooltipList);
            foreach (var tooltip in k_TooltipList)
            {
                ShowTooltip(tooltip);
            }
        }

        void OnRayHovering(GameObject gameObject, RayEventData eventData)
        {
            if (gameObject == m_ModuleParent)
                return;

            k_TooltipList.Clear();
            gameObject.GetComponents(k_TooltipList);
            foreach (var tooltip in k_TooltipList)
            {
                ShowTooltip(tooltip);
            }
        }

        void OnRayExited(GameObject gameObject, RayEventData eventData)
        {
            if (gameObject && gameObject != m_ModuleParent)
            {
                k_TooltipList.Clear();
                gameObject.GetComponents(k_TooltipList);
                foreach (var tooltip in k_TooltipList)
                {
                    HideTooltip(tooltip);
                }
            }
        }

        public void ShowTooltip(ITooltip tooltip, bool persistent = false, float duration = 0f, ITooltipPlacement placementOverride = null, Action becameVisible = null)
        {
            if (!IsValidTooltip(tooltip))
                return;

            TooltipData data;
            if (m_Tooltips.TryGetValue(tooltip, out data))
            {
                // Compare the targets to see if they changed
                var currentTarget = data.GetTooltipTarget(tooltip);
                var placement = data.placement;
                var currentPlacement = placement;

                data.persistent |= persistent;
                data.placement = placementOverride ?? tooltip as ITooltipPlacement;

                // Set the text to new text
                var tooltipUI = data.tooltipUI;
                if (tooltipUI)
                {
                    tooltipUI.Show(tooltip.tooltipText, placement.tooltipAlignment);

                    var newTarget = data.GetTooltipTarget(tooltip);
                    if (currentTarget != newTarget)
                    {
                        // Get the different between the 'old' tooltip position and 'new' tooltip position, even taking alignment into account
                        var transitionLerp = 1.0f - Mathf.Clamp01((Time.time - data.transitionTime) / k_ChangeTransitionDuration);
                        var currentPosition = currentTarget.TransformPoint(GetTooltipOffset(tooltipUI, currentPlacement, data.transitionOffset * transitionLerp));
                        var newPosition = newTarget.TransformPoint(GetTooltipOffset(tooltipUI, placement, Vector3.zero));

                        // Store it as an additional offset that we'll quickly lerp from
                        data.transitionOffset = newTarget.InverseTransformVector(currentPosition - newPosition);
                        data.transitionTime = Time.time;
                    }

                    if (duration > 0)
                    {
                        data.duration = duration;
                        data.lastModifiedTime = Time.time;
                    }
                }

                return;
            }

            // Negative durations only affect existing tooltips
            if (duration < 0)
                return;

            var tooltipData = GetTooltipData();

            tooltipData.startTime = Time.time;
            tooltipData.lastModifiedTime = Time.time;
            tooltipData.persistent = persistent;
            tooltipData.duration = duration;
            tooltipData.becameVisible = becameVisible;
            tooltipData.placement = placementOverride ?? tooltip as ITooltipPlacement;
            tooltipData.orientationWeight = 0.0f;
            tooltipData.transitionOffset = Vector3.zero;
            tooltipData.transitionTime = 0.0f;

            m_Tooltips[tooltip] = tooltipData;
        }

        TooltipData GetTooltipData()
        {
            if (m_TooltipDataPool.Count > 0)
            {
                var tooltipData = m_TooltipDataPool.Dequeue();
                tooltipData.Reset();
                return tooltipData;
            }

            return new TooltipData();
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

                if (m_ModuleParent.activeInHierarchy && tooltipData.tooltipUI)
                    EditorMonoBehaviour.instance.StartCoroutine(AnimateHide(tooltip, tooltipData));
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

            if (placement != null)
            {
                tooltipUI.rectTransform.GetWorldCorners(k_Corners);
                var halfWidth = (k_Corners[0] - k_Corners[2]).magnitude * 0.5f;
                offset *= halfWidth;
            }
            else
            {
                offset = k_Offset * this.GetViewerScale() * Vector3.back;
            }

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
            m_TooltipDataPool.Enqueue(tooltipData);
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var visibilitySubscriber = obj as IFunctionalitySubscriber<IProvidesSetTooltipVisibility>;
            if (visibilitySubscriber != null)
                visibilitySubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
