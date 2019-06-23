using System.Collections;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    public class HintLine : MonoBehaviour
    {
        const string k_ShaderLineRadiusPropertyName = "_lineRadius";

#pragma warning disable 649
        [SerializeField]
        VRLineRenderer m_ScrollLineRenderer;

        [SerializeField]
        Color m_VisibleColor = Color.white;

        [SerializeField]
        Color m_PulseColor = Color.white;

        [SerializeField]
        MeshRenderer m_MeshRenderer;
#pragma warning restore 649

        Coroutine m_ScrollArrowPulseCoroutine;
        float m_PulseDuration;
        Material m_HintLineMaterial;

        /// <summary>
        /// Set the width of the line visuals
        /// </summary>
        public float LineWidth { set { m_ScrollLineRenderer.SetWidth(value, value); } }

        /// <summary>
        /// Set the start & end positions for the line visuals
        /// </summary>
        public Vector3[] Positions { set { m_ScrollLineRenderer.SetPositions(value); } }

        void Awake()
        {
            m_ScrollLineRenderer.SetVertexCount(4);
            m_ScrollLineRenderer.useWorldSpace = true;
            m_ScrollLineRenderer.SetWidth(0f, 0f);
            m_HintLineMaterial = MaterialUtils.GetMaterialClone(m_MeshRenderer);
        }

        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_HintLineMaterial);
        }

        /// <summary>
        /// Perform an animated visual pulsing of color
        /// </summary>
        public void PulseColor()
        {
            if (Mathf.Approximately(m_PulseDuration, 0f) || m_PulseDuration > 0.85f)
                this.RestartCoroutine(ref m_ScrollArrowPulseCoroutine, AnimatePulseColor());
        }

        IEnumerator AnimatePulseColor()
        {
            const float kTargetDuration = 1f;
            m_PulseDuration = 0f;
            var maxShaderLineRadius = new Vector3(0.03f, 0f, 100f);
            var minShaderLineRadius = new Vector3(0.005f, 0f, 100f);
            var currentVector3ShaderLineRadius = m_HintLineMaterial.GetVector(k_ShaderLineRadiusPropertyName);
            var currentColor = m_ScrollLineRenderer.colorStart; // The line stand & end colors are the same; fetch only one of them
            while (m_PulseDuration < kTargetDuration)
            {
                var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
                var newColor = Color.Lerp(currentColor, m_PulseColor, shapedDuration);
                m_ScrollLineRenderer.SetColors(newColor, newColor);
                m_PulseDuration += Time.unscaledDeltaTime * 5;
                m_HintLineMaterial.SetVector(k_ShaderLineRadiusPropertyName, Vector3.Lerp(currentVector3ShaderLineRadius, maxShaderLineRadius, shapedDuration));
                yield return null;
            }

            while (m_PulseDuration > 0f)
            {
                var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
                var newColor = Color.Lerp(m_VisibleColor, m_PulseColor, shapedDuration);
                m_ScrollLineRenderer.SetColors(newColor, newColor);
                m_PulseDuration -= Time.unscaledDeltaTime * 1.5f;
                m_HintLineMaterial.SetVector(k_ShaderLineRadiusPropertyName, Vector3.Lerp(minShaderLineRadius, maxShaderLineRadius, shapedDuration));
                yield return null;
            }

            m_ScrollLineRenderer.SetColors(m_VisibleColor, m_VisibleColor);
            m_PulseDuration = 0f;
        }
    }
}
