using System.Collections;
using Unity.Labs.EditorXR.Extensions;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.EditorXR.Workspaces
{
    sealed class WorkspaceHighlight : MonoBehaviour
    {
        const string k_TopColorProperty = "_ColorTop";
        const string k_BottomColorProperty = "_ColorBottom";
        const string k_AlphaProperty = "_Alpha";

        Coroutine m_HighlightCoroutine;
        Material m_TopHighlightMaterial;

#pragma warning disable 649
        [SerializeField]
        MeshRenderer m_TopHighlightRenderer;
#pragma warning restore 649

        public bool visible
        {
            get { return m_HighlightVisible; }
            set
            {
                if (m_HighlightVisible == value)
                    return;

                m_HighlightVisible = value;

                this.StopCoroutine(ref m_HighlightCoroutine);

                if (m_HighlightVisible)
                    m_HighlightCoroutine = StartCoroutine(ShowHighlight());
                else
                    m_HighlightCoroutine = StartCoroutine(HideHighlight());
            }
        }

        bool m_HighlightVisible;

        void Awake()
        {
            m_TopHighlightMaterial = MaterialUtils.GetMaterialClone(m_TopHighlightRenderer);
            m_TopHighlightMaterial.SetColor(k_TopColorProperty, UnityBrandColorScheme.sessionGradient.a);
            m_TopHighlightMaterial.SetColor(k_BottomColorProperty, UnityBrandColorScheme.sessionGradient.b);
            m_TopHighlightMaterial.SetFloat(k_AlphaProperty, 0f); // hide the highlight initially
        }

        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_TopHighlightMaterial);
        }

        IEnumerator ShowHighlight()
        {
            const float kTargetAlpha = 1f;
            var currentAlpha = m_TopHighlightMaterial.GetFloat(k_AlphaProperty);
            var smoothVelocity = 0f;
            var currentDuration = 0f;
            const float kTargetDuration = 0.3f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                currentAlpha = MathUtilsExt.SmoothDamp(currentAlpha, kTargetAlpha, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_TopHighlightMaterial.SetFloat(k_AlphaProperty, currentAlpha);
                yield return null;
            }

            m_TopHighlightMaterial.SetFloat(k_AlphaProperty, kTargetAlpha); // set value after loop because precision matters in this case
            m_HighlightCoroutine = null;
        }

        IEnumerator HideHighlight()
        {
            const float kTargetAlpha = 0f;
            var currentAlpha = m_TopHighlightMaterial.GetFloat(k_AlphaProperty);
            var smoothVelocity = 0f;
            var currentDuration = 0f;
            const float kTargetDuration = 0.35f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                currentAlpha = MathUtilsExt.SmoothDamp(currentAlpha, kTargetAlpha, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_TopHighlightMaterial.SetFloat(k_AlphaProperty, currentAlpha);
                yield return null;
            }

            m_TopHighlightMaterial.SetFloat(k_AlphaProperty, kTargetAlpha); // set value after loop because precision matters in this case
            m_HighlightCoroutine = null;
        }
    }
}
