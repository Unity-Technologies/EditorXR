using System.Collections;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class UndoMenuUI : MonoBehaviour, IConnectInterfaces
    {
        const float k_EngageAnimationDuration = 0.1f;
        const float k_EngagedAlpha = 0.5f;
        const float k_DisengagedAlpha = 0.3f;
        const float k_UndoPerformedAlpha = 1f;
        const string k_MaterialColorProperty = "_Color";

#pragma warning disable 649
        [SerializeField]
        MeshRenderer m_UndoButtonMeshRenderer;

        [SerializeField]
        MeshRenderer m_RedoButtonMeshRenderer;
#pragma warning restore 649

        Material m_UndoButtonMaterial;
        Material m_RedoButtonMaterial;
        Coroutine m_EngageCoroutine;
        Coroutine m_UndoPerformedCoroutine;

        bool m_Engaged;
        bool m_Visible;

        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                if (m_AlternateMenuOrigin == value)
                    return;

                m_AlternateMenuOrigin = value;
                transform.SetParent(m_AlternateMenuOrigin);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        Transform m_AlternateMenuOrigin;

        public bool engaged
        {
            get { return m_Engaged; }
            set
            {
                if (m_Engaged == value)
                    return;

                m_Engaged = value;
                this.RestartCoroutine(ref m_EngageCoroutine, AnimateEngage(m_Engaged));
            }
        }

        public bool visible
        {
            get { return m_Visible; }
            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;

                StopAllCoroutines();
                gameObject.SetActive(value);
            }
        }

        void Awake()
        {
            m_UndoButtonMaterial = MaterialUtils.GetMaterialClone(m_UndoButtonMeshRenderer);
            m_RedoButtonMaterial = MaterialUtils.GetMaterialClone(m_RedoButtonMeshRenderer);
        }

        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_UndoButtonMaterial);
            UnityObjectUtils.Destroy(m_RedoButtonMaterial);
        }

        IEnumerator AnimateEngage(bool engaging)
        {
            var undoStartingColor = m_UndoButtonMaterial.GetColor(k_MaterialColorProperty);
            var redoStartingColor = m_RedoButtonMaterial.GetColor(k_MaterialColorProperty);
            var targetColor = Color.white;
            targetColor.a = engaging ? k_EngagedAlpha : k_DisengagedAlpha;
            var transitionAmount = 0f;
            var currentDuration = 0f;
            while (transitionAmount < 1f)
            {
                var smoothAmont = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
                m_UndoButtonMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(undoStartingColor, targetColor, smoothAmont));
                m_RedoButtonMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(redoStartingColor, targetColor, smoothAmont));
                currentDuration += Time.deltaTime;
                transitionAmount = currentDuration / k_EngageAnimationDuration;
                yield return null;
            }

            m_UndoButtonMaterial.SetColor(k_MaterialColorProperty, targetColor);
            m_RedoButtonMaterial.SetColor(k_MaterialColorProperty, targetColor);
        }

        public void StartPerformedAnimation(bool undo)
        {
            StopCoroutine(m_EngageCoroutine);
            this.RestartCoroutine(ref m_UndoPerformedCoroutine, AnimateUndoPerformed(undo));
        }

        IEnumerator AnimateUndoPerformed(bool undo)
        {
            var targetMaterial = undo ? m_UndoButtonMaterial : m_RedoButtonMaterial;
            var startingColor = m_UndoButtonMaterial.GetColor(k_MaterialColorProperty);
            var targetColor = startingColor;
            targetColor.a = k_UndoPerformedAlpha;
            var transitionAmount = 0f;
            var currentDuration = 0f;
            while (transitionAmount < 1f)
            {
                var currentColor = Color.Lerp(startingColor, targetColor, MathUtilsExt.SmoothInOutLerpFloat(transitionAmount));
                targetMaterial.SetColor(k_MaterialColorProperty, currentColor);
                currentDuration += Time.deltaTime;
                transitionAmount = currentDuration / k_EngageAnimationDuration;
                yield return null;
            }

            transitionAmount = currentDuration = 0f;
            startingColor = targetColor;
            targetColor.a = k_DisengagedAlpha;
            while (transitionAmount < 1f)
            {
                var currentColor = Color.Lerp(startingColor, targetColor, MathUtilsExt.SmoothInOutLerpFloat(transitionAmount));
                targetMaterial.SetColor(k_MaterialColorProperty, currentColor);
                currentDuration += Time.deltaTime;
                transitionAmount = currentDuration / k_EngageAnimationDuration;
                yield return null;
            }

            targetMaterial.SetColor(k_MaterialColorProperty, targetColor);
        }
    }
}
