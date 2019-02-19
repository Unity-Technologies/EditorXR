using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEditor.Experimental.EditorVR.Extensions;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class KeyboardButton : BaseHandle
    {
        public enum SelectionState
        {
            Normal,
            Highlighted,
            Pressed,
            Disabled
        }

        const float k_RepeatTime = 0.35f;
        const float k_RepeatDecayFactor = 0.75f;
        const float k_ClickTime = 0.3f;
        const float k_PressEmission = 1f;
        const float k_EmissionLerpTime = 0.1f;
        const float k_KeyResponseDuration = 0.1f;
        const float k_KeyResponsePositionAmplitude = 0.02f;
        const float k_KeyResponseScaleAmplitude = 0.08f;

        public TextMeshProUGUI textComponent
        {
            get { return m_TextComponent; }
            set { m_TextComponent = value; }
        }

        [SerializeField]
        TextMeshProUGUI m_TextComponent;

        public Material targetMeshMaterial
        {
            get { return m_TargetMeshMaterial; }
        }

        Material m_TargetMeshMaterial;

        public Color targetMeshBaseColor
        {
            get { return m_TargetMeshBaseColor; }
        }

        Color m_TargetMeshBaseColor;

        public CanvasGroup canvasGroup
        {
            get
            {
                return !m_CanvasGroup
                    ? GetComponentInChildren<CanvasGroup>(true)
                    : m_CanvasGroup;
            }
        }

        CanvasGroup m_CanvasGroup;

        [SerializeField]
        char m_Character;

        [SerializeField]
        bool m_UseShiftCharacter;

        [SerializeField]
        char m_ShiftCharacter;

        bool m_ShiftMode;

        [SerializeField]
        Renderer m_TargetMesh;

        Vector3 m_TargetMeshInitialScale;
        Vector3 m_TargetMeshInitialLocalPosition;

        [SerializeField]
        bool m_RepeatOnHold;

        [SerializeField]
        WorkspaceButton m_WorkspaceButton;

        float m_HoldStartTime;
        float m_RepeatWaitTime;
        float m_PressDownTime;
        bool m_Holding;
        bool m_Triggered; // Hit by mallet
        Coroutine m_ChangeEmissionCoroutine;
        Coroutine m_PunchKeyCoroutine;
        Coroutine m_SetTextAlphaCoroutine;
        Coroutine m_MoveCoroutine;

        Action<char> m_KeyPress;
        Func<bool> m_PressOnHover;
        Func<bool> m_InTransition;

        void Awake()
        {
            var targetMeshTransform = m_TargetMesh.transform;
            m_TargetMeshInitialLocalPosition = targetMeshTransform.localPosition;
            m_TargetMeshInitialScale = targetMeshTransform.localScale;
            m_TargetMeshMaterial = MaterialUtils.GetMaterialClone(m_TargetMesh.GetComponent<Renderer>());
            m_TargetMeshBaseColor = m_TargetMeshMaterial.color;
            m_CanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }

        /// <summary>
        /// Initiallize this key
        /// </summary>
        /// <param name="keyPress">Method to be invoked when the key is pressed</param>
        /// <param name="pressOnHover">Method to be invoked to determine key behaviour</param>
        public void Setup(Action<char> keyPress, Func<bool> pressOnHover, Func<bool> inTransition)
        {
            m_PressOnHover = pressOnHover;
            m_KeyPress = keyPress;
            m_InTransition = inTransition;
        }

        /// <summary>
        /// Enable or disable shift mode for this key
        /// </summary>
        /// <param name="active">Set to true to enable shift, false to disable</param>
        public void SetShiftModeActive(bool active)
        {
            if (!m_UseShiftCharacter) return;

            m_ShiftMode = active;

            if (textComponent != null)
            {
                if (m_ShiftMode && m_ShiftCharacter != 0)
                {
                    textComponent.text = m_ShiftCharacter.ToString();
                }
                else
                {
                    if (textComponent.text.Length > 1)
                        textComponent.text = textComponent.text.ToLower();
                    else
                        textComponent.text = m_Character.ToString();
                }
            }
        }

        /// <summary>
        /// Move the key to a target position
        /// </summary>
        /// <param name="targetPos">The position to move to</param>
        /// <param name="duration">The duration of the movement</param>
        public void MoveToPosition(Vector3 targetPos, float duration)
        {
            this.StopCoroutine(ref m_MoveCoroutine);
            m_MoveCoroutine = StartCoroutine(MoveToPositionOverTime(targetPos, duration));
        }

        IEnumerator MoveToPositionOverTime(Vector3 targetPos, float duration)
        {
            var currentPosition = transform.position;
            var transitionAmount = 0f;
            var speed = 0f;
            var currentDuration = 0f;
            while (currentDuration < duration)
            {
                currentDuration += Time.deltaTime;
                transitionAmount = MathUtilsExt.SmoothDamp(transitionAmount, 1f, ref speed, duration, Mathf.Infinity, Time.deltaTime);
                transform.position = Vector3.Lerp(currentPosition, targetPos, transitionAmount);
                yield return null;
            }

            transform.position = targetPos;
            m_MoveCoroutine = null;
        }

        /// <summary>
        /// Set the alpha value of the button text
        /// </summary>
        /// <param name="alpha">The final alpha value of the key text</param>
        /// <param name="duration">The lerp time</param>
        public void SetTextAlpha(float alpha, float duration)
        {
            this.StopCoroutine(ref m_SetTextAlphaCoroutine);

            if (isActiveAndEnabled)
                m_SetTextAlphaCoroutine = StartCoroutine(SetAlphaOverTime(alpha, duration));
            else
                m_CanvasGroup.alpha = alpha;
        }

        IEnumerator SetAlphaOverTime(float toAlpha, float duration)
        {
            var startingAlpha = canvasGroup.alpha;
            var t = 0f;
            while (t < duration)
            {
                var a = t / duration;
                canvasGroup.alpha = Mathf.Lerp(startingAlpha, toAlpha, a);
                t += Time.deltaTime;
                yield return null;
            }

            m_CanvasGroup.alpha = toAlpha;

            m_SetTextAlphaCoroutine = null;
        }

        protected override void OnHandleHoverStarted(HandleEventData eventData)
        {
            base.OnHandleHoverStarted(eventData);

            if (!m_InTransition())
            {
                if ((KeyCode)m_Character == KeyCode.Escape || m_ShiftMode && (KeyCode)m_ShiftCharacter == KeyCode.Escape)
                {
                    var gradient = new GradientPair();
                    gradient.a = UnityBrandColorScheme.red;
                    gradient.b = UnityBrandColorScheme.redDark;
                    m_WorkspaceButton.SetMaterialColors(gradient);
                }
                else
                {
                    m_WorkspaceButton.ResetColors();
                }

                m_WorkspaceButton.highlighted = true;
            }
        }

        protected override void OnHandleHoverEnded(HandleEventData eventData)
        {
            base.OnHandleHoverEnded(eventData);

            m_WorkspaceButton.highlighted = false;
        }

        protected override void OnHandleDragStarted(HandleEventData eventData)
        {
            if (eventData == null)
                return;

            m_PressDownTime = Time.realtimeSinceStartup;

            if (m_RepeatOnHold)
                KeyPressed();

            m_WorkspaceButton.highlighted = true;

            base.OnHandleDragStarted(eventData);
        }

        protected override void OnHandleDragging(HandleEventData eventData)
        {
            if (eventData == null)
                return;

            if (m_RepeatOnHold)
                HoldKey();
            else if (Time.realtimeSinceStartup - m_PressDownTime > k_ClickTime)
                m_WorkspaceButton.highlighted = false;

            base.OnHandleDragging(eventData);
        }

        protected override void OnHandleDragEnded(HandleEventData eventData)
        {
            if (eventData == null)
                return;

            if (m_RepeatOnHold)
                EndKeyHold();
            else if (Time.realtimeSinceStartup - m_PressDownTime < k_ClickTime)
                KeyPressed();

            base.OnHandleDragEnded(eventData);
        }

        public void OnTriggerEnter(Collider col)
        {
            if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null || m_InTransition())
                return;

            if (transform.InverseTransformPoint(col.transform.position).z > 0f)
                return;

            m_Triggered = true;
            m_WorkspaceButton.pressed = true;

            KeyPressed();
        }

        public void OnTriggerStay(Collider col)
        {
            if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null || m_InTransition())
                return;

            if (m_RepeatOnHold && m_Triggered)
                HoldKey();
        }

        public void OnTriggerExit(Collider col)
        {
            if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null)
                return;

            if (m_RepeatOnHold && m_Triggered)
                EndKeyHold();

            m_WorkspaceButton.pressed = false;

            m_Triggered = false;
        }

        private void KeyPressed()
        {
            if (m_KeyPress == null || m_InTransition()) return;

            if (m_ShiftMode && m_ShiftCharacter != 0)
                m_KeyPress(m_ShiftCharacter);
            else
                m_KeyPress(m_Character);

            if (!m_Holding)
            {
                this.StopCoroutine(ref m_ChangeEmissionCoroutine);
                m_ChangeEmissionCoroutine = StartCoroutine(IncreaseEmission());

                this.StopCoroutine(ref m_PunchKeyCoroutine);
                m_PunchKeyCoroutine = StartCoroutine(PushKeyMesh());
            }

            if (m_RepeatOnHold)
                StartKeyHold();
        }

        private void StartKeyHold()
        {
            m_Holding = true;
            m_HoldStartTime = Time.realtimeSinceStartup;
            m_RepeatWaitTime = k_RepeatTime;
        }

        private void HoldKey()
        {
            if (m_Holding && m_HoldStartTime + m_RepeatWaitTime < Time.realtimeSinceStartup)
            {
                KeyPressed();
                m_HoldStartTime = Time.realtimeSinceStartup;
                m_RepeatWaitTime *= k_RepeatDecayFactor;
            }
        }

        private void EndKeyHold()
        {
            m_Holding = false;

            this.StopCoroutine(ref m_ChangeEmissionCoroutine);
            m_ChangeEmissionCoroutine = StartCoroutine(DecreaseEmission());

            this.StopCoroutine(ref m_PunchKeyCoroutine);
            m_PunchKeyCoroutine = StartCoroutine(LiftKeyMesh());
        }

        private void OnDisable()
        {
            InstantClearState();
        }

        void InstantClearState()
        {
            var finalColor = Color.white * Mathf.LinearToGammaSpace(0f);
            m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);

            m_TargetMeshMaterial.color = m_TargetMeshBaseColor;

            m_TargetMesh.transform.localScale = m_TargetMeshInitialScale;
            m_TargetMesh.transform.localPosition = m_TargetMeshInitialLocalPosition;

            m_WorkspaceButton.InstantClearState();
        }

        private void OnDestroy()
        {
            ObjectUtils.Destroy(m_TargetMeshMaterial);
        }

        IEnumerator IncreaseEmission()
        {
            var t = 0f;
            Color finalColor;
            while (t < k_EmissionLerpTime)
            {
                var emission = t / k_EmissionLerpTime;
                finalColor = Color.white * Mathf.LinearToGammaSpace(emission * k_PressEmission);
                m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);
                t += Time.deltaTime;

                yield return null;
            }

            finalColor = Color.white * Mathf.LinearToGammaSpace(k_PressEmission);
            m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);

            if (!m_Holding)
                m_ChangeEmissionCoroutine = StartCoroutine(DecreaseEmission());
            else
                m_ChangeEmissionCoroutine = null;
        }

        IEnumerator DecreaseEmission()
        {
            var t = 0f;
            Color finalColor;
            while (t < k_EmissionLerpTime)
            {
                var emission = 1f - t / k_EmissionLerpTime;
                finalColor = Color.white * Mathf.LinearToGammaSpace(emission * k_PressEmission);
                m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);
                t += Time.deltaTime;

                yield return null;
            }
            finalColor = Color.white * Mathf.LinearToGammaSpace(0f);
            m_TargetMeshMaterial.SetColor("_EmissionColor", finalColor);

            m_ChangeEmissionCoroutine = null;
        }

        IEnumerator PushKeyMesh()
        {
            var targetMeshTransform = m_TargetMesh.transform;
            targetMeshTransform.localPosition = m_TargetMeshInitialLocalPosition;

            var elapsedTime = 0f;
            while (elapsedTime < k_KeyResponseDuration)
            {
                elapsedTime += Time.deltaTime;
                var t = Mathf.Clamp01(elapsedTime / k_KeyResponseDuration);

                targetMeshTransform.localScale = m_TargetMeshInitialScale + m_TargetMeshInitialScale * t * k_KeyResponsePositionAmplitude;

                var pos = m_TargetMeshInitialLocalPosition;
                pos.z = t * k_KeyResponsePositionAmplitude;
                targetMeshTransform.localPosition = pos;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            targetMeshTransform.localScale = m_TargetMeshInitialScale + m_TargetMeshInitialScale * k_KeyResponseScaleAmplitude;
            var finalPos = m_TargetMeshInitialLocalPosition;
            finalPos.z = k_KeyResponsePositionAmplitude;
            targetMeshTransform.localPosition = finalPos;

            if (!m_Holding)
                m_PunchKeyCoroutine = StartCoroutine(LiftKeyMesh());
            else
                m_PunchKeyCoroutine = null;
        }

        IEnumerator LiftKeyMesh()
        {
            var targetMeshTransform = m_TargetMesh.transform;
            targetMeshTransform.localPosition = m_TargetMeshInitialLocalPosition;

            var elapsedTime = 0f;
            while (elapsedTime < k_KeyResponseDuration)
            {
                elapsedTime += Time.deltaTime;
                var t = 1f - Mathf.Clamp01(elapsedTime / k_KeyResponseDuration);

                targetMeshTransform.localScale = m_TargetMeshInitialScale + m_TargetMeshInitialScale * t * k_KeyResponseScaleAmplitude;

                var pos = m_TargetMeshInitialLocalPosition;
                pos.z = t * k_KeyResponsePositionAmplitude;
                targetMeshTransform.localPosition = pos;

                yield return null;
            }

            targetMeshTransform.localScale = m_TargetMeshInitialScale;
            targetMeshTransform.localPosition = m_TargetMeshInitialLocalPosition;
            m_PunchKeyCoroutine = null;
        }
    }
}
