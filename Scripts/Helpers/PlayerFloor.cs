#if UNITY_EDITOR
using System.Collections;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    [ExecuteInEditMode]
    public class PlayerFloor : MonoBehaviour, IUsesViewerScale, IDetectGazeDivergence
    {
        const float k_XOffset = 0.05f;
        const float k_ZOffset = 0.2f;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

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
            const float kLerpMultiplier = 4f;
            var currentScale = this.GetViewerScale();
            m_FloorPosition.x = m_Camera.position.x + k_XOffset * currentScale;
            m_FloorPosition.z = m_Camera.position.z - k_ZOffset * currentScale;
            m_FloorPosition.y = m_CameraRig.transform.position.y;
            m_FloorPosition -= VRView.headCenteredOrigin * currentScale;
            transform.position = m_FloorPosition;
            m_CameraForwardTarget = m_Camera.transform.XZForward();
            m_CameraForwardCurrent = Vector3.Lerp(m_CameraForwardCurrent, m_CameraForwardTarget, Time.unscaledDeltaTime * kLerpMultiplier);
            transform.forward = m_CameraForwardCurrent;

            const float kAllowedDegreeOfGazeDivergence = 10f;
            var visible = this.IsAboveDivergenceThreshold(transform, kAllowedDegreeOfGazeDivergence);
            //Debug.LogWarning("<color=yellow> Out of FOV : </color>" + visible);

            if (m_Visible != visible)
                this.RestartCoroutine(ref m_AnimationVisibilityCoroutine, UpdateVisibility(visible));
        }

        IEnumerator UpdateVisibility(bool visible)
        {
            m_Visible = visible;

            const float kDurationMultiplier = 1.5f;
            var currentCanvasGroupAlpha = m_CanvasGroup.alpha;
            var targetCanvasGroupAlpha = visible ? 0f : 1f;
            var amount = 0f;
            while (amount < 1f)
            {
                var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(amount += Time.unscaledDeltaTime * kDurationMultiplier);
                m_CanvasGroup.alpha = Mathf.Lerp(currentCanvasGroupAlpha, targetCanvasGroupAlpha, shapedAmount);
                yield return null;
            }

            m_CanvasGroup.alpha = targetCanvasGroupAlpha;
            m_AnimationVisibilityCoroutine = null;
        }
    }
}
#endif
