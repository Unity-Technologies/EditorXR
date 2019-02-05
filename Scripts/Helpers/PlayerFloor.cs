﻿#if UNITY_EDITOR
using System.Collections;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    [ExecuteInEditMode]
    public class PlayerFloor : MonoBehaviour, IUsesViewerScale, IDetectGazeDivergence
    {
        const float k_XOffset = 0.05f;
        const float k_ZOffset = 0.025f;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        Image m_BorderImage;

        Vector3 m_FloorPosition;
        Transform m_Camera;
        Transform m_CameraRig;

        Vector3 m_CameraForwardCurrent;
        Vector3 m_CameraForwardTarget;

        bool m_Visible;
        Coroutine m_AnimationVisibilityCoroutine;

        void Awake()
        {
            m_Camera = CameraUtils.GetMainCamera().transform;
            m_CameraRig = CameraUtils.GetCameraRig();
        }

        void Update()
        {
            const float kLerpMultiplier = 6f;
            var currentScale = this.GetViewerScale();
            m_FloorPosition.x = m_Camera.position.x + k_XOffset * currentScale;
            m_FloorPosition.z = m_Camera.position.z - k_ZOffset * currentScale;
            m_FloorPosition.y = m_CameraRig.transform.position.y;
            m_FloorPosition -= VRView.headCenteredOrigin * currentScale;
            transform.position = m_FloorPosition;
            m_CameraForwardTarget = m_Camera.transform.XZForward();
            m_CameraForwardCurrent = Vector3.Lerp(m_CameraForwardCurrent, m_CameraForwardTarget, Time.unscaledDeltaTime * kLerpMultiplier);
            transform.forward = m_CameraForwardCurrent;

            const float kAllowedDegreeOfGazeDivergence = 25f;
            var isAboveDivergenceThreshold = this.IsAboveDivergenceThreshold(transform, kAllowedDegreeOfGazeDivergence);

            if (m_Visible != isAboveDivergenceThreshold)
                this.RestartCoroutine(ref m_AnimationVisibilityCoroutine, UpdateVisibility(isAboveDivergenceThreshold));
        }

        IEnumerator UpdateVisibility(bool hidden)
        {
            m_Visible = hidden;

            const float kHidingSpeedMultiplier = 4f;
            const float kShowingSpeedMultiplier = 0.5f;
            var durationMultiplier = hidden ? kHidingSpeedMultiplier : kShowingSpeedMultiplier;
            var currentCanvasGroupAlpha = m_CanvasGroup.alpha;
            var targetCanvasGroupAlpha = hidden ? 0f : 1f;
            var amount = 0f;
            while (amount < 1f)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(amount += Time.unscaledDeltaTime * durationMultiplier);
                var targetZeroOneLerpValue = Mathf.Lerp(currentCanvasGroupAlpha, targetCanvasGroupAlpha, shapedAmount);
                m_CanvasGroup.alpha = targetZeroOneLerpValue;
                m_BorderImage.fillAmount = targetZeroOneLerpValue;
                yield return null;
            }

            m_CanvasGroup.alpha = targetCanvasGroupAlpha;
            m_AnimationVisibilityCoroutine = null;
        }
    }
}
#endif