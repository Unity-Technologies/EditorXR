using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Extensions;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using Unity.Labs.XR;
using UnityEngine;

namespace Unity.Labs.EditorXR.Proxies
{
    sealed class DefaultProxyRay : MonoBehaviour, IUsesViewerScale
    {
        struct DefaultRayVisibilitySettings
        {
            public int priority;
            public bool rayVisible;
            public bool coneVisible;
        }

#pragma warning disable 649
        [SerializeField]
        XRLineRenderer m_LineRenderer;

        [SerializeField]
        GameObject m_Tip;

        [SerializeField]
        float m_LineWidth;

        [SerializeField]
        MeshFilter m_Cone;
#pragma warning restore 649

        Vector3 m_TipStartScale;
        Transform m_ConeTransform;
        Vector3 m_OriginalConeLocalScale;
        Coroutine m_RayVisibilityCoroutine;
        Coroutine m_ConeVisibilityCoroutine;
        Material m_RayMaterial;
        float m_LastPointerLength;

        readonly Dictionary<object, DefaultRayVisibilitySettings> m_VisibilitySettings = new Dictionary<object, DefaultRayVisibilitySettings>();

        /// <summary>
        /// The length of the direct selection pointer
        /// </summary>
        public float pointerLength
        {
            get
            {
                if (!coneVisible || m_ConeVisibilityCoroutine != null)
                    return m_LastPointerLength;

                m_LastPointerLength = (m_Cone.transform.TransformPoint(m_Cone.sharedMesh.bounds.size.z * Vector3.forward) - m_Cone.transform.position).magnitude;
                return m_LastPointerLength;
            }
        }

        public bool rayVisible { get; private set; }
        public bool coneVisible { get; private set; }

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        void OnDisable()
        {
            this.StopCoroutine(ref m_RayVisibilityCoroutine);
            this.StopCoroutine(ref m_ConeVisibilityCoroutine);
        }

        public void AddVisibilitySettings(object caller, bool rayVisible, bool coneVisible, int priority = 0)
        {
            m_VisibilitySettings[caller] = new DefaultRayVisibilitySettings { rayVisible = rayVisible, coneVisible = coneVisible, priority = priority };
        }

        public void RemoveVisibilitySettings(object caller)
        {
            m_VisibilitySettings.Remove(caller);
        }

        public void SetLength(float length)
        {
            if (!rayVisible)
                return;

            var viewerScale = this.GetViewerScale();
            var scaledWidth = m_LineWidth * viewerScale;
            var scaledLength = length / viewerScale;

            var lineRendererTransform = m_LineRenderer.transform;
            lineRendererTransform.localScale = Vector3.one * scaledLength;
            m_LineRenderer.widthStart = scaledWidth;
            m_LineRenderer.widthEnd = scaledWidth * scaledLength;
            var thisTransform = transform;
            m_Tip.transform.position = thisTransform.position + thisTransform.forward * length;
            m_Tip.transform.localScale = scaledLength * m_TipStartScale;
        }

        public void SetColor(Color c)
        {
            m_RayMaterial.color = c;
        }

        void Awake()
        {
            m_RayMaterial = MaterialUtils.GetMaterialClone(m_LineRenderer.GetComponent<MeshRenderer>());
            m_ConeTransform = m_Cone.transform;
            m_OriginalConeLocalScale = m_ConeTransform.localScale;

            rayVisible = true;
            coneVisible = true;
        }

        void Start()
        {
            m_TipStartScale = m_Tip.transform.localScale;
            rayVisible = true;
        }

        void Update()
        {
            UpdateVisibility();
        }

        void UpdateVisibility()
        {
            var coneVisible = true;
            var rayVisible = true;

            if (m_VisibilitySettings.Count > 0)
            {
                var maxPriority = 0;
                foreach (var kvp in m_VisibilitySettings)
                {
                    var priority = kvp.Value.priority;
                    if (priority > maxPriority)
                        maxPriority = priority;
                }

                foreach (var kvp in m_VisibilitySettings)
                {
                    var settings = kvp.Value;
                    if (settings.priority == maxPriority)
                    {
                        rayVisible &= settings.rayVisible;
                        coneVisible &= settings.coneVisible;
                    }
                }
            }

            if (this.rayVisible != rayVisible)
            {
                this.rayVisible = rayVisible;
                this.StopCoroutine(ref m_RayVisibilityCoroutine);
                m_RayVisibilityCoroutine = StartCoroutine(rayVisible ? ShowRay() : HideRay());
            }

            if (this.coneVisible != coneVisible)
            {
                this.coneVisible = coneVisible;
                this.StopCoroutine(ref m_ConeVisibilityCoroutine);
                m_ConeVisibilityCoroutine = StartCoroutine(coneVisible ? ShowCone() : HideCone());
            }
        }

        IEnumerator HideRay()
        {
            m_Tip.transform.localScale = Vector3.zero;

            // cache current width for smooth animation to target value without snapping
            var currentWidth = m_LineRenderer.widthStart;
            const float kTargetWidth = 0f;
            const float kSmoothTime = 0.1875f;
            var smoothVelocity = 0f;
            var currentDuration = 0f;
            while (currentDuration < kSmoothTime)
            {
                currentDuration += Time.deltaTime;
                currentWidth = MathUtilsExt.SmoothDamp(currentWidth, kTargetWidth, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
                m_LineRenderer.widthStart = currentWidth;
                m_LineRenderer.widthEnd = currentWidth;
                yield return null;
            }

            m_LineRenderer.widthStart = kTargetWidth;
            m_LineRenderer.widthEnd = kTargetWidth;
            m_RayVisibilityCoroutine = null;
        }

        IEnumerator ShowRay()
        {
            m_Tip.transform.localScale = m_TipStartScale;

            var viewerScale = this.GetViewerScale();
            float scaledWidth;
            var currentWidth = m_LineRenderer.widthStart / viewerScale;
            var smoothVelocity = 0f;
            const float kSmoothTime = 0.3125f;
            var currentDuration = 0f;
            while (currentDuration < kSmoothTime)
            {
                viewerScale = this.GetViewerScale();
                currentDuration += Time.deltaTime;
                currentWidth = MathUtilsExt.SmoothDamp(currentWidth, m_LineWidth, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
                scaledWidth = currentWidth * viewerScale;
                m_LineRenderer.widthStart = scaledWidth;
                m_LineRenderer.widthEnd = scaledWidth;
                yield return null;
            }

            viewerScale = this.GetViewerScale();
            scaledWidth = m_LineWidth * viewerScale;
            m_LineRenderer.widthStart = scaledWidth;
            m_LineRenderer.widthEnd = scaledWidth;
            m_RayVisibilityCoroutine = null;
        }

        IEnumerator HideCone()
        {
            var currentScale = m_ConeTransform.localScale;
            var smoothVelocity = Vector3.one;
            const float kSmoothTime = 0.1875f;
            var currentDuration = 0f;
            while (currentDuration < kSmoothTime)
            {
                currentDuration += Time.deltaTime;
                currentScale = MathUtilsExt.SmoothDamp(currentScale, Vector3.zero, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
                m_ConeTransform.localScale = currentScale;
                yield return null;
            }

            m_ConeTransform.localScale = Vector3.zero;
            m_ConeVisibilityCoroutine = null;
        }

        IEnumerator ShowCone()
        {
            var currentScale = m_ConeTransform.localScale;
            var smoothVelocity = Vector3.zero;
            const float kSmoothTime = 0.3125f;
            var currentDuration = 0f;
            while (currentDuration < kSmoothTime)
            {
                currentDuration += Time.deltaTime;
                currentScale = MathUtilsExt.SmoothDamp(currentScale, m_OriginalConeLocalScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
                m_ConeTransform.localScale = currentScale;
                yield return null;
            }

            m_ConeTransform.localScale = m_OriginalConeLocalScale;
            m_ConeVisibilityCoroutine = null;
        }

        public Color GetColor()
        {
            return m_RayMaterial.color;
        }

        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_RayMaterial);
        }
    }
}
