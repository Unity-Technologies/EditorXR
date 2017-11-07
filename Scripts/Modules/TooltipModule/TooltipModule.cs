#if UNITY_EDITOR
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

        const int k_PoolInitialCapacity = 16;

        const string k_MaterialColorTopProperty = "_ColorTop";
        const string k_MaterialColorBottomProperty = "_ColorBottom";

        static readonly Quaternion k_FlipYRotation = Quaternion.AngleAxis(180f, Vector3.up);
        static readonly Quaternion k_FlipZRotation = Quaternion.AngleAxis(180f, Vector3.forward);

        [SerializeField]
        GameObject m_TooltipPrefab;

        [SerializeField]
        GameObject m_TooltipCanvasPrefab;

        [SerializeField]
        Material m_HighlightMaterial;

        [SerializeField]
        Material m_TooltipBackgroundMaterial;

        class TooltipData
        {
            public float startTime;
            public float lastModifiedTime;
            public TooltipUI tooltipUI;
            public Material customHighlightMaterial;
            public bool persistent;
            public float duration;
            public ITooltipPlacement placement;

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
        Color m_OriginalBackgroundColor;

        // Local method use only -- created here to reduce garbage collection
        static readonly List<ITooltip> k_TooltipsToRemove = new List<ITooltip>();
        static readonly List<ITooltip> k_TooltipList = new List<ITooltip>();
        static readonly List<TooltipUI> k_TooltipUIs = new List<TooltipUI>();

        void Start()
        {
            m_TooltipCanvas = Instantiate(m_TooltipCanvasPrefab).transform;
            m_TooltipCanvas.SetParent(transform);
            m_TooltipScale = m_TooltipPrefab.transform.localScale;
            m_HighlightMaterial = Instantiate(m_HighlightMaterial);
            m_TooltipBackgroundMaterial = Instantiate(m_TooltipBackgroundMaterial);
            m_OriginalBackgroundColor = m_TooltipBackgroundMaterial.color;
            var sessionGradient = UnityBrandColorScheme.sessionGradient;
            m_HighlightMaterial.SetColor(k_MaterialColorTopProperty, sessionGradient.a);
            m_HighlightMaterial.SetColor(k_MaterialColorBottomProperty, sessionGradient.b);
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
                        tooltipData.tooltipUI = tooltipUI;
                        tooltipUI.highlight.material = tooltipData.customHighlightMaterial ?? m_HighlightMaterial;
                        tooltipUI.background.material = m_TooltipBackgroundMaterial;
                        var tooltipTransform = tooltipUI.transform;
                        MathUtilsExt.SetTransformOffset(target, tooltipTransform, Vector3.zero, Quaternion.identity);
                        tooltipTransform.localScale = Vector3.zero;

                        var hasLine = placement != null;
                        tooltipUI.dottedLine.gameObject.SetActive(hasLine);
                        foreach (var sphere in tooltipUI.spheres)
                        {
                            sphere.gameObject.SetActive(hasLine);
                        }
                    }

                    var lerp = Mathf.Clamp01((hoverTime - k_Delay) / k_TransitionDuration);
                    UpdateVisuals(tooltip, tooltipUI, placement, target, lerp);
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

            var tooltipObject = Instantiate(m_TooltipPrefab, m_TooltipCanvas);
            tooltipObject.GetComponents(k_TooltipUIs);

            var tooltipUI = k_TooltipUIs[0]; // We expect exactly one TooltipUI on the prefab root

            return tooltipUI;
        }

        void UpdateVisuals(ITooltip tooltip, TooltipUI tooltipUI, ITooltipPlacement placement, Transform target, float lerp)
        {
            var tooltipTransform = tooltipUI.transform;

            lerp = MathUtilsExt.SmoothInOutLerpFloat(lerp); // shape the lerp for better presentation

            var tooltipText = tooltipUI.text;
            if (tooltipText)
            {
                tooltipText.text = tooltip.tooltipText;
                tooltipText.color = Color.Lerp(Color.clear, Color.white, lerp);
            }

            var viewerScale = this.GetViewerScale();
            tooltipTransform.localScale = m_TooltipScale * lerp * viewerScale;

            m_TooltipBackgroundMaterial.SetColor("_Color", Color.Lerp(UnityBrandColorScheme.darker, m_OriginalBackgroundColor, lerp));

            // Adjust for alignment
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

            var rectTransform = tooltipUI.GetComponent<RectTransform>();
            var rect = rectTransform.rect;
            var halfWidth = rect.width * 0.5f;
            var halfHeight = rect.height * 0.5f;

            if (placement != null)
                offset *= halfWidth * rectTransform.lossyScale.x;
            else
                offset = Vector3.back * k_Offset * this.GetViewerScale();

            var rotationOffset = Quaternion.identity;
            var cameraForward = CameraUtils.GetMainCamera().transform.forward;
            if (Vector3.Dot(cameraForward, target.forward) < 0)
                rotationOffset *= k_FlipYRotation;

            var upDot = Vector3.Dot(Vector3.up, target.up);
            if (Mathf.Abs(Vector3.Dot(Vector3.forward, target.up)) > Mathf.Abs(upDot))
            {
                if (Vector3.Dot(cameraForward, target.up) < 0)
                    rotationOffset *= k_FlipZRotation;
            }
            else
            {
                if (upDot < 0)
                    rotationOffset *= k_FlipZRotation;
            }

            MathUtilsExt.SetTransformOffset(target, tooltipTransform, offset * lerp, rotationOffset);

            if (placement != null)
            {
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

        public void ShowTooltip(ITooltip tooltip, bool persistent = false, float duration = 0f, ITooltipPlacement placement = null)
        {
            if (!IsValidTooltip(tooltip))
                return;

            TooltipData data;
            if (m_Tooltips.TryGetValue(tooltip, out data))
            {
                data.persistent |= persistent;
                data.placement = placement ?? tooltip as ITooltipPlacement;
                data.customHighlightMaterial = GetHighlightMaterial(tooltip);

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
                customHighlightMaterial = GetHighlightMaterial(tooltip),
                startTime = Time.time,
                lastModifiedTime = Time.time,
                persistent = persistent,
                duration = duration,
                placement = placement ?? tooltip as ITooltipPlacement
            };
        }

        Material GetHighlightMaterial(ITooltip tooltip)
        {
            Material highlightMaterial = null;
            var customTooltipColor = tooltip as ISetCustomTooltipColor;
            if (customTooltipColor != null)
            {
                highlightMaterial = Instantiate(m_HighlightMaterial);
                var customTooltipHighlightColor = customTooltipColor.customTooltipHighlightColor;
                highlightMaterial.SetColor(k_MaterialColorTopProperty, customTooltipHighlightColor.a);
                highlightMaterial.SetColor(k_MaterialColorBottomProperty, customTooltipHighlightColor.b);
            }

            return highlightMaterial;
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
            var placement = data.placement;
            var target = data.GetTooltipTarget(tooltip);
            var tooltipUI = data.tooltipUI;
            var startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < k_TransitionDuration)
            {
                if (!target)
                    break;

                UpdateVisuals(tooltip, tooltipUI, placement, target,
                    1 - (Time.realtimeSinceStartup - startTime) / k_TransitionDuration);
                yield return null;
            }

            RecycleTooltip(tooltipUI);
        }

        void RecycleTooltip(TooltipUI tooltipUI)
        {
            tooltipUI.gameObject.SetActive(false);
            m_TooltipPool.Enqueue(tooltipUI);
        }
    }
}
#endif
