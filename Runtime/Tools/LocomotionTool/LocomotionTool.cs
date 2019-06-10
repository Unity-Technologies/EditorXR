using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class LocomotionTool : MonoBehaviour, ITool, ILocomotor, IUsesRayOrigin, IUsesRayVisibilitySettings,
        ICustomActionMap, ILinkedObject, IUsesViewerScale, ISettingsMenuItemProvider, ISerializePreferences,
        IUsesDeviceType, IGetVRPlayerObjects, IBlockUIInteraction, IUsesRequestFeedback, IUsesNode, IUsesFunctionalityInjection
    {
        [Serializable]
        class Preferences
        {
            [SerializeField]
            bool m_BlinkMode;

            public bool blinkMode
            {
                get { return m_BlinkMode; }
                set { m_BlinkMode = value; }
            }
        }

        const float k_FastMoveSpeed = 20f;
        const float k_SlowMoveSpeed = 1f;
        const float k_RotationDamping = 0.2f;
        const float k_RotationThreshold = 0.75f;
        const float k_DistanceThreshold = 0.02f;

        //TODO: Fix triangle intersection test at tiny scales, so this can go back to 0.01
        const float k_MinScale = 0.1f;
        const float k_MaxScale = 1000f;

        const float k_RingDirectionSmoothing = 0.5f;
        const float k_MouseMovementMultiplier = 0.01f;
        const float k_MouseScrollMultiplier = 0.01f;
        const float k_MouseRotationMultiplier = 0.05f;

        const string k_Crawl = "Crawl";
        const string k_Rotate = "Rotate";
        const string k_Blink = "Blink";
        const string k_Fly = "Fly";

        const int k_RotationSegments = 32;

        static readonly Vector3 k_RingOffset = new Vector3(0f, -0.3f, 0.5f);

#pragma warning disable 649
        [SerializeField]
        GameObject m_BlinkVisualsPrefab;

        [SerializeField]
        GameObject m_ViewerScaleVisualsPrefab;

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        GameObject m_SettingsMenuItemPrefab;

        [SerializeField]
        GameObject m_RingPrefab;
#pragma warning restore 649

        Preferences m_Preferences;

        ViewerScaleVisuals m_ViewerScaleVisuals;

        GameObject m_BlinkVisualsGO;
        BlinkVisuals m_BlinkVisuals;

        bool m_AllowScaling = true;
        bool m_Scaling;
        float m_StartScale;
        float m_StartDistance;
        Vector3 m_StartPosition;
        Vector3 m_StartMidPoint;
        Vector3 m_StartDirection;
        float m_StartYaw;

        bool m_Rotating;
        bool m_StartCrawling;
        bool m_Crawling;
        bool m_WasRotating;
        float m_CrawlStartTime;
        Vector3 m_ActualRayOriginStartPosition;
        Vector3 m_RayOriginStartPosition;
        Vector3 m_RayOriginStartForward;
        Vector3 m_RayOriginStartRight;
        Quaternion m_RigStartRotation;
        Vector3 m_RigStartPosition;
        Vector3 m_CameraStartPosition;
        Quaternion m_LastRotationDiff;

        bool m_BlinkMoving;

        // Allow shared updater to check input values and consume controls
        LocomotionInput m_LocomotionInput;

        Toggle m_FlyToggle;
        Toggle m_BlinkToggle;
        bool m_BlockValueChangedListener;

        bool m_MouseWasHeld;
        Vector3 m_RingDirection;
        Ring m_Ring;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<ProxyFeedbackRequest> m_MainButtonFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_SpeedFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_CrawlFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_ScaleFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_RotateFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_ResetScaleFeedback = new List<ProxyFeedbackRequest>();

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get { return false; } }
        public Transform rayOrigin { get; set; }
        public Transform cameraRig { private get; set; }
        public List<ILinkedObject> linkedObjects { private get; set; }
        public Node node { private get; set; }

        public GameObject settingsMenuItemPrefab
        {
            get { return m_SettingsMenuItemPrefab; }
        }

        public GameObject settingsMenuItemInstance
        {
            set
            {
                if (value == null)
                {
                    m_FlyToggle = null;
                    m_BlinkToggle = null;
                    return;
                }

                var defaultToggleGroup = value.GetComponentInChildren<DefaultToggleGroup>();
                foreach (var toggle in value.GetComponentsInChildren<Toggle>())
                {
                    if (toggle == defaultToggleGroup.defaultToggle)
                    {
                        m_FlyToggle = toggle;
                        toggle.onValueChanged.AddListener(isOn =>
                        {
                            if (m_BlockValueChangedListener)
                                return;

                            // m_Preferences on all instances refer
                            m_Preferences.blinkMode = !isOn;
                            foreach (var linkedObject in linkedObjects)
                            {
                                var locomotionTool = (LocomotionTool)linkedObject;
                                if (locomotionTool != this)
                                {
                                    locomotionTool.m_BlockValueChangedListener = true;

                                    //linkedObject.m_ToggleGroup.NotifyToggleOn(isOn ? m_FlyToggle : m_BlinkToggle);
                                    // HACK: Toggle Group claims these toggles are not a part of the group
                                    locomotionTool.m_FlyToggle.isOn = isOn;
                                    locomotionTool.m_BlinkToggle.isOn = !isOn;
                                    locomotionTool.m_BlockValueChangedListener = false;
                                }
                            }
                        });
                    }
                    else
                    {
                        m_BlinkToggle = toggle;
                    }
                }
            }
        }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
        IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
        IProvidesRayVisibilitySettings IFunctionalitySubscriber<IProvidesRayVisibilitySettings>.provider { get; set; }
#endif

        void Start()
        {
            if (this.IsSharedUpdater(this))
            {
                if (m_Preferences == null)
                {
                    m_Preferences = new Preferences();

                    // Share one preferences object across all instances
                    foreach (var linkedObject in linkedObjects)
                    {
                        ((LocomotionTool)linkedObject).m_Preferences = m_Preferences;
                    }
                }

                var instance = EditorXRUtils.Instantiate(m_RingPrefab, cameraRig, false);
                m_Ring = instance.GetComponent<Ring>();
            }

            m_BlinkVisualsGO = EditorXRUtils.Instantiate(m_BlinkVisualsPrefab, rayOrigin);
            m_BlinkVisuals = m_BlinkVisualsGO.GetComponentInChildren<BlinkVisuals>();
            this.InjectFunctionalitySingle(m_BlinkVisuals);
            m_BlinkVisuals.ignoreList = this.GetVRPlayerObjects();
            m_BlinkVisualsGO.SetActive(false);
            m_BlinkVisualsGO.transform.parent = rayOrigin;
            m_BlinkVisualsGO.transform.localPosition = Vector3.zero;
            m_BlinkVisualsGO.transform.localRotation = Quaternion.identity;

            var viewerScaleObject = EditorXRUtils.Instantiate(m_ViewerScaleVisualsPrefab, cameraRig, false);
            m_ViewerScaleVisuals = viewerScaleObject.GetComponent<ViewerScaleVisuals>();
            this.InjectFunctionalitySingle(m_ViewerScaleVisuals);
            viewerScaleObject.SetActive(false);

            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        void SetRingPosition()
        {
            if (!this.IsSharedUpdater(this))
                return;

            var cameraTransform = CameraUtils.GetMainCamera().transform;
            var cameraYaw = cameraTransform.localRotation.ConstrainYaw();
            if (!m_Ring)
                return;

            var ringTransform = m_Ring.transform;
            ringTransform.localPosition = cameraTransform.localPosition + cameraYaw * k_RingOffset;
            ringTransform.localRotation = cameraYaw;
        }

        void OnDestroy()
        {
            this.RemoveRayVisibilitySettings(rayOrigin, this);
            this.ClearFeedbackRequests(this);

            if (m_ViewerScaleVisuals)
                UnityObjectUtils.Destroy(m_ViewerScaleVisuals.gameObject);

            if (m_Ring)
                UnityObjectUtils.Destroy(m_Ring.gameObject);
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            m_LocomotionInput = (LocomotionInput)input;

            this.SetUIBlockedForRayOrigin(rayOrigin, true);

            if (DoTwoHandedScaling(consumeControl))
            {
                if (m_Preferences.blinkMode && m_LocomotionInput.blink.isHeld)
                    m_BlinkVisuals.visible = false;

                return;
            }

            if (DoRotating(consumeControl))
                return;

            if (m_Preferences.blinkMode)
            {
                if (DoBlink(consumeControl))
                    return;
            }
            else
            {
                if (DoFlying(consumeControl))
                    return;
            }

            if (DoCrawl(consumeControl))
                return;

            this.SetUIBlockedForRayOrigin(rayOrigin, false);
        }

        // HACK: Because we don't get mouse input through action maps, just use Update
        void Update()
        {
            var mouseDelta = VRView.MouseDelta;

            if (VRView.LeftMouseButtonHeld)
            {
                if (!m_MouseWasHeld)
                    SetRingPosition();

                var xzConstrain = new Vector3(1f, 0f, 1f);
                var forward = Vector3.Scale(cameraRig.forward, xzConstrain).normalized;
                var right = new Vector3(-forward.z, 0f, forward.x);
                var delta = (mouseDelta.x * right + mouseDelta.y * forward)
                    * k_MouseMovementMultiplier;

                cameraRig.position += delta;

                if (this.IsSharedUpdater(this) && delta.normalized != Vector3.zero)
                {
                    m_RingDirection = Vector3.Lerp(m_RingDirection, delta.normalized, k_RingDirectionSmoothing);

                    if (m_RingDirection != Vector3.zero)
                        m_Ring.SetEffectWorldDirection(m_RingDirection);
                }
            }

            if (VRView.RightMouseButtonHeld)
                cameraRig.rotation *= Quaternion.AngleAxis(mouseDelta.x * k_MouseRotationMultiplier, Vector3.up);

            var deltaScroll = VRView.MouseScrollDelta.y;
            cameraRig.position += deltaScroll * Vector3.up * k_MouseScrollMultiplier * this.GetViewerScale();

            if (this.IsSharedUpdater(this) && !Mathf.Approximately(deltaScroll, 0f))
            {
                if (!m_Ring.coreVisible)
                    SetRingPosition();

                m_Ring.SetEffectCore();

                if (deltaScroll > 0f)
                    m_Ring.SetEffectCoreUp();
                else
                    m_Ring.SetEffectCoreDown();
            }

            m_MouseWasHeld = VRView.LeftMouseButtonHeld;
        }

        bool DoFlying(ConsumeControlDelegate consumeControl)
        {
            foreach (var linkedObject in linkedObjects)
            {
                var locomotionTool = (LocomotionTool)linkedObject;
                if (locomotionTool.m_LocomotionInput != null && locomotionTool.m_LocomotionInput.crawl.isHeld)
                    return false;
            }

            var forwardControl = m_LocomotionInput.forward;
            var reverseControl = m_LocomotionInput.reverse;
            if (forwardControl.wasJustPressed || reverseControl.wasJustPressed)
            {
                foreach (var linkedObject in linkedObjects)
                {
                    var locomotionTool = (LocomotionTool)linkedObject;
                    if (locomotionTool == this)
                    {
                        locomotionTool.HideMainButtonFeedback();
                        locomotionTool.ShowRotateFeedback();
                    }

                    locomotionTool.HideCrawlFeedback();
                }
            }

            if (forwardControl.wasJustReleased || reverseControl.wasJustReleased)
            {
                var otherControlHeld = false;
                foreach (var linkedObject in linkedObjects)
                {
                    var locomotionTool = (LocomotionTool)linkedObject;
                    if (locomotionTool == this)
                        continue;

                    var input = locomotionTool.m_LocomotionInput;
                    if (input.forward.isHeld || input.reverse.isHeld)
                    {
                        otherControlHeld = true;
                        break;
                    }
                }

                foreach (var linkedObject in linkedObjects)
                {
                    var locomotionTool = (LocomotionTool)linkedObject;
                    if (locomotionTool == this)
                    {
                        locomotionTool.HideSpeedFeedback();
                        locomotionTool.HideRotateFeedback();
                        locomotionTool.ShowMainButtonFeedback();
                    }

                    if (!otherControlHeld)
                        locomotionTool.ShowCrawlFeedback();
                }
            }

            var reverse = reverseControl.isHeld;
            var moving = forwardControl.isHeld || reverse;
            if (moving)
            {
                var speed = k_SlowMoveSpeed;
                var speedControl = m_LocomotionInput.speed;
                var speedControlValue = speedControl.value;
                if (!Mathf.Approximately(speedControlValue, 0)) // Consume control to block selection
                {
                    speed = k_SlowMoveSpeed + speedControlValue * (k_FastMoveSpeed - k_SlowMoveSpeed);
                    HideSpeedFeedback();
                }
                else if (m_SpeedFeedback.Count == 0)
                {
                    ShowSpeedFeedback();
                }

                speed *= this.GetViewerScale();
                if (reverse)
                    speed *= -1;

                m_Rotating = false;
                cameraRig.Translate(Quaternion.Inverse(cameraRig.rotation) * rayOrigin.forward * speed * Time.unscaledDeltaTime);

                consumeControl(speedControl); // Consume trigger to block block-select
                consumeControl(forwardControl);
                return true;
            }

            return false;
        }

        bool DoRotating(ConsumeControlDelegate consumeControl)
        {
            var reverse = m_LocomotionInput.reverse.isHeld;
            var move = m_LocomotionInput.forward.isHeld || reverse;
            if (move)
            {
                if (m_LocomotionInput.rotate.isHeld)
                {
                    foreach (var linkedObject in linkedObjects)
                    {
                        var locomotionTool = (LocomotionTool)linkedObject;
                        locomotionTool.HideRotateFeedback();
                        locomotionTool.HideCrawlFeedback();
                        locomotionTool.HideScaleFeedback();
                        locomotionTool.HideSpeedFeedback();
                        if (!m_Preferences.blinkMode)
                            locomotionTool.HideMainButtonFeedback();
                    }

                    var localRayRotation = Quaternion.Inverse(cameraRig.rotation) * rayOrigin.rotation;
                    var localRayForward = localRayRotation * Vector3.forward;
                    if (Mathf.Abs(Vector3.Dot(localRayForward, Vector3.up)) > k_RotationThreshold)
                        return true;

                    localRayForward.y = 0;
                    localRayForward.Normalize();
                    if (!m_Rotating)
                    {
                        m_Rotating = true;
                        m_WasRotating = true;
                        m_RigStartPosition = cameraRig.position;
                        m_RigStartRotation = cameraRig.rotation;

                        m_RayOriginStartForward = localRayForward;
                        m_RayOriginStartRight = localRayRotation * (reverse ? Vector3.right : Vector3.left);
                        m_RayOriginStartRight.y = 0;
                        m_RayOriginStartRight.Normalize();

                        m_CameraStartPosition = CameraUtils.GetMainCamera().transform.position;
                        m_LastRotationDiff = Quaternion.identity;
                    }

                    var startOffset = m_RigStartPosition - m_CameraStartPosition;

                    var angle = Vector3.Angle(m_RayOriginStartForward, localRayForward);
                    var dot = Vector3.Dot(m_RayOriginStartRight, localRayForward);
                    var rotation = Quaternion.Euler(0, angle * Mathf.Sign(dot), 0);
                    var filteredRotation = Quaternion.Lerp(m_LastRotationDiff, rotation, k_RotationDamping);

                    const float segmentSize = 360f / k_RotationSegments;
                    var segmentedRotation = Quaternion.Euler(0, Mathf.Round(filteredRotation.eulerAngles.y / segmentSize) * segmentSize, 0);

                    cameraRig.rotation = m_RigStartRotation * segmentedRotation;
                    cameraRig.position = m_CameraStartPosition + segmentedRotation * startOffset;

                    m_LastRotationDiff = filteredRotation;
                    m_BlinkVisuals.visible = false;

                    m_StartCrawling = false;
                    m_Crawling = false;
                    return true;
                }
            }

            if (!m_LocomotionInput.rotate.isHeld)
            {
                if (m_WasRotating)
                {
                    foreach (var linkedObject in linkedObjects)
                    {
                        var locomotionTool = (LocomotionTool)linkedObject;
                        var input = locomotionTool.m_LocomotionInput;
                        if (input.blink.isHeld)
                        {
                            locomotionTool.ShowSpeedFeedback();
                            locomotionTool.ShowRotateFeedback();
                        }
                        else
                        {
                            if (locomotionTool == this)
                                locomotionTool.ShowAltRotateFeedback();
                            else if (!input.crawl.isHeld)
                                locomotionTool.ShowMainButtonFeedback();
                        }
                    }
                }

                m_WasRotating = false;
            }

            m_Rotating = false;
            return false;
        }

        bool DoCrawl(ConsumeControlDelegate consumeControl)
        {
            foreach (var linkedObject in linkedObjects)
            {
                if (((LocomotionTool)linkedObject).m_Rotating)
                    return false;
            }

            var crawl = m_LocomotionInput.crawl;
            var blink = m_LocomotionInput.blink;
            if (!m_LocomotionInput.forward.isHeld && !blink.isHeld && crawl.isHeld)
            {
                if (!m_StartCrawling && !m_WasRotating)
                {
                    m_StartCrawling = true;
                    m_ActualRayOriginStartPosition = m_RayOriginStartPosition;
                    m_CrawlStartTime = Time.time;

                    foreach (var linkedObject in linkedObjects)
                    {
                        ((LocomotionTool)linkedObject).HideCrawlFeedback();
                        ((LocomotionTool)linkedObject).HideMainButtonFeedback();
                    }
                }

                var localRayPosition = cameraRig.position - rayOrigin.position;
                var distance = Vector3.Distance(m_ActualRayOriginStartPosition, localRayPosition);
                var distanceThreshold = distance > k_DistanceThreshold * this.GetViewerScale();
                var timeThreshold = Time.time > m_CrawlStartTime + UIUtils.DoubleClickIntervalMax;
                if (!m_Crawling && m_StartCrawling && (timeThreshold || distanceThreshold))
                {
                    m_Crawling = true;
                    m_RigStartPosition = cameraRig.position;
                    m_RayOriginStartPosition = m_RigStartPosition - rayOrigin.position;
                    this.AddRayVisibilitySettings(rayOrigin, this, false, false);
                }

                if (m_Crawling)
                    cameraRig.position = m_RigStartPosition + localRayPosition - m_RayOriginStartPosition;

                if (m_RotateFeedback.Count == 0)
                {
                    HideMainButtonFeedback();
                    ShowAltRotateFeedback();
                }

                if (m_ScaleFeedback.Count == 0)
                    ShowScaleFeedback();

                return true;
            }

            this.RemoveRayVisibilitySettings(rayOrigin, this);

            if (crawl.isHeld && blink.wasJustReleased || crawl.wasJustReleased)
            {
                var otherCrawlHeld = false;
                foreach (var linkedObject in linkedObjects)
                {
                    var locomotionTool = (LocomotionTool)linkedObject;
                    if (locomotionTool == this)
                        continue;

                    if (locomotionTool.m_LocomotionInput.crawl.isHeld)
                    {
                        otherCrawlHeld = true;
                        break;
                    }
                }

                if (!otherCrawlHeld)
                {
                    HideRotateFeedback();
                    HideScaleFeedback();
                    foreach (var linkedObject in linkedObjects)
                    {
                        var locomotionTool = (LocomotionTool)linkedObject;
                        locomotionTool.ShowCrawlFeedback();
                        locomotionTool.ShowMainButtonFeedback();
                    }
                }
            }

            m_StartCrawling = false;
            m_Crawling = false;
            return false;
        }

        bool DoBlink(ConsumeControlDelegate consumeControl)
        {
            var blink = m_LocomotionInput.blink;
            if (blink.wasJustPressed)
            {
                HideMainButtonFeedback();
                HideCrawlFeedback();
                ShowRotateFeedback();
            }

            if (blink.isHeld)
            {
                this.AddRayVisibilitySettings(rayOrigin, this, false, false);
                var speedControl = m_LocomotionInput.speed;
                var speed = speedControl.value;
                m_BlinkVisuals.extraSpeed = speed;

                if (speed < 0)
                    HideSpeedFeedback();
                else if (m_SpeedFeedback.Count == 0)
                    ShowSpeedFeedback();

                m_BlinkVisuals.visible = true;

                consumeControl(speedControl); // Consume trigger to block block-select
                consumeControl(blink);
                return true;
            }

            if (blink.wasJustReleased)
            {
                this.RemoveRayVisibilitySettings(rayOrigin, this);
                HideRotateFeedback();
                HideSpeedFeedback();
                ShowMainButtonFeedback();
                ShowCrawlFeedback();

                m_BlinkVisuals.visible = false;

                if (m_BlinkVisuals.targetPosition.HasValue)
                    StartCoroutine(MoveTowardTarget(m_BlinkVisuals.targetPosition.Value));

                return true;
            }

            this.RemoveRayVisibilitySettings(rayOrigin, this);

            return m_BlinkMoving;
        }

        bool DoTwoHandedScaling(ConsumeControlDelegate consumeControl)
        {
            foreach (var linkedObject in linkedObjects)
            {
                if (((LocomotionTool)linkedObject).m_Rotating)
                    return false;
            }

            if (this.IsSharedUpdater(this))
            {
                var crawl = m_LocomotionInput.crawl;
                if (crawl.isHeld)
                {
                    if (m_AllowScaling)
                    {
                        var otherGripHeld = false;
                        foreach (var linkedObject in linkedObjects)
                        {
                            var otherLocomotionTool = (LocomotionTool)linkedObject;
                            if (otherLocomotionTool == this)
                                continue;

                            var otherLocomotionInput = otherLocomotionTool.m_LocomotionInput;
                            if (otherLocomotionInput == null) // This can occur if crawl is pressed when EVR is opened
                                continue;

                            var otherCrawl = otherLocomotionInput.crawl;
                            if (otherCrawl.isHeld)
                            {
                                otherGripHeld = true;
                                consumeControl(crawl);
                                consumeControl(otherCrawl);

                                // Also consume thumbstick axes to disable radial menu
                                consumeControl(m_LocomotionInput.horizontal);
                                consumeControl(m_LocomotionInput.vertical);
                                consumeControl(otherLocomotionInput.horizontal);
                                consumeControl(otherLocomotionInput.vertical);

                                // Pre-emptively consume thumbstick press to override UndoMenu
                                consumeControl(m_LocomotionInput.scaleReset);
                                consumeControl(otherLocomotionInput.scaleReset);

                                // Also pre-emptively consume world-reset
                                consumeControl(m_LocomotionInput.worldReset);
                                consumeControl(otherLocomotionInput.worldReset);

                                var thisPosition = cameraRig.InverseTransformPoint(rayOrigin.position);
                                var otherRayOrigin = otherLocomotionTool.rayOrigin;
                                var otherPosition = cameraRig.InverseTransformPoint(otherRayOrigin.position);
                                var distance = Vector3.Distance(thisPosition, otherPosition);

                                this.AddRayVisibilitySettings(rayOrigin, this, false, false);
                                this.AddRayVisibilitySettings(otherRayOrigin, this, false, false);

                                var rayToRay = otherPosition - thisPosition;
                                var midPoint = thisPosition + rayToRay * 0.5f;

                                rayToRay.y = 0; // Use for yaw rotation

                                var pivotYaw = cameraRig.rotation.ConstrainYaw();

                                if (!m_Scaling)
                                {
                                    m_StartScale = this.GetViewerScale();
                                    m_StartDistance = distance;
                                    m_StartMidPoint = pivotYaw * midPoint * m_StartScale;
                                    m_StartPosition = cameraRig.position;
                                    m_StartDirection = rayToRay;
                                    m_StartYaw = cameraRig.rotation.eulerAngles.y;

                                    otherLocomotionTool.m_Scaling = true;
                                    otherLocomotionTool.m_Crawling = false;
                                    otherLocomotionTool.m_StartCrawling = false;

                                    m_ViewerScaleVisuals.leftHand = rayOrigin;
                                    m_ViewerScaleVisuals.rightHand = otherRayOrigin;
                                    m_ViewerScaleVisuals.gameObject.SetActive(true);

                                    foreach (var obj in linkedObjects)
                                    {
                                        var locomotionTool = (LocomotionTool)obj;
                                        locomotionTool.HideScaleFeedback();
                                        locomotionTool.HideRotateFeedback();
                                        locomotionTool.HideMainButtonFeedback();
                                        locomotionTool.ShowResetScaleFeedback();
                                    }
                                }

                                m_Scaling = true;
                                m_StartCrawling = false;
                                m_Crawling = false;

                                var currentScale = Mathf.Clamp(m_StartScale * (m_StartDistance / distance), k_MinScale, k_MaxScale);

                                var scaleReset = m_LocomotionInput.scaleReset;
                                var scaleResetHeld = scaleReset.isHeld;

                                var otherScaleReset = otherLocomotionInput.scaleReset;
                                var otherScaleResetHeld = otherScaleReset.isHeld;

                                // Press both thumb buttons to reset scale
                                if (scaleResetHeld && otherScaleResetHeld)
                                {
                                    m_AllowScaling = false;

                                    rayToRay = otherRayOrigin.position - rayOrigin.position;
                                    midPoint = rayOrigin.position + rayToRay * 0.5f;
                                    var currOffset = midPoint - cameraRig.position;

                                    cameraRig.position = midPoint - currOffset / currentScale;
                                    cameraRig.rotation = Quaternion.AngleAxis(m_StartYaw, Vector3.up);

                                    ResetViewerScale();
                                }

                                var worldReset = m_LocomotionInput.worldReset;
                                var worldResetHeld = worldReset.isHeld;
                                if (worldResetHeld)
                                    consumeControl(worldReset);

                                var otherWorldReset = otherLocomotionInput.worldReset;
                                var otherWorldResetHeld = otherWorldReset.isHeld;
                                if (otherWorldResetHeld)
                                    consumeControl(otherWorldReset);

                                // Press both triggers to reset to origin
                                if (worldResetHeld && otherWorldResetHeld)
                                {
                                    m_AllowScaling = false;
                                    cameraRig.position = VRView.headCenteredOrigin;
                                    cameraRig.rotation = Quaternion.identity;

                                    ResetViewerScale();
                                }

                                if (m_AllowScaling)
                                {
                                    var yawSign = Mathf.Sign(Vector3.Dot(Quaternion.AngleAxis(90, Vector3.down) * m_StartDirection, rayToRay));
                                    var currentYaw = m_StartYaw + Vector3.Angle(m_StartDirection, rayToRay) * yawSign;
                                    var currentRotation = Quaternion.AngleAxis(currentYaw, Vector3.up);
                                    midPoint = currentRotation * midPoint * currentScale;

                                    var pos = m_StartPosition + m_StartMidPoint - midPoint;
                                    cameraRig.position = pos;

                                    cameraRig.rotation = currentRotation;

                                    this.SetViewerScale(currentScale);
                                }
                                break;
                            }
                        }

                        if (!otherGripHeld)
                            CancelScale();
                    }
                }
                else
                {
                    CancelScale();
                }
            }

            return m_Scaling;
        }

        void ResetViewerScale()
        {
            this.SetViewerScale(1f);
            m_ViewerScaleVisuals.gameObject.SetActive(false);
        }

        void CancelScale()
        {
            m_AllowScaling = true;
            m_Scaling = false;

            foreach (var linkedObject in linkedObjects)
            {
                var locomotionTool = (LocomotionTool)linkedObject;
                if (!locomotionTool.m_Crawling && !locomotionTool.m_BlinkVisuals.gameObject.activeInHierarchy)
                {
                    var rayOrigin = locomotionTool.rayOrigin;
                    this.RemoveRayVisibilitySettings(rayOrigin, this);
                }

                locomotionTool.m_Scaling = false;
                locomotionTool.HideResetScaleFeedback();
            }

            m_ViewerScaleVisuals.gameObject.SetActive(false);
        }

        IEnumerator MoveTowardTarget(Vector3 targetPosition)
        {
            m_BlinkMoving = true;

            var offset = cameraRig.position - CameraUtils.GetMainCamera().transform.position;
            offset.y = 0;
            offset += VRView.headCenteredOrigin * this.GetViewerScale();

            targetPosition += offset;
            const float kTargetDuration = 0.05f;
            var currentPosition = cameraRig.position;
            var currentDuration = 0f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.unscaledDeltaTime;
                currentPosition = Vector3.Lerp(currentPosition, targetPosition, currentDuration / kTargetDuration);
                cameraRig.position = currentPosition;
                yield return null;
            }

            cameraRig.position = targetPosition;
            m_BlinkMoving = false;
        }

        void ShowFeedback(List<ProxyFeedbackRequest> requests, string controlName, string tooltipText = null)
        {
            if (tooltipText == null)
                tooltipText = controlName;

            List<VRInputDevice.VRControl> ids;
            if (m_Controls.TryGetValue(controlName, out ids))
            {
                foreach (var id in ids)
                {
                    var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);
                    request.node = node;
                    request.control = id;
                    request.tooltipText = tooltipText;
                    requests.Add(request);
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowCrawlFeedback()
        {
            ShowFeedback(m_CrawlFeedback, k_Crawl);
        }

        void ShowMainButtonFeedback()
        {
            ShowFeedback(m_MainButtonFeedback, k_Blink, m_Preferences.blinkMode ? k_Blink : k_Fly);
        }

        void ShowRotateFeedback()
        {
            ShowFeedback(m_RotateFeedback, k_Rotate);
        }

        void ShowAltRotateFeedback()
        {
            ShowFeedback(m_RotateFeedback, k_Blink, k_Rotate);
        }

        void ShowScaleFeedback()
        {
            List<VRInputDevice.VRControl> ids;
            if (m_Controls.TryGetValue(k_Crawl, out ids))
            {
                foreach (var id in ids)
                {
                    var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);
                    request.control = id;
                    request.node = node == Node.LeftHand ? Node.RightHand : Node.LeftHand;
                    request.tooltipText = "Scale";
                    m_ScaleFeedback.Add(request);
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowResetScaleFeedback()
        {
            ShowFeedback(m_ResetScaleFeedback, "ScaleReset", "Reset scale");
            ShowFeedback(m_ResetScaleFeedback, "WorldReset", "Reset position rotation and scale");
        }

        void ShowSpeedFeedback()
        {
            ShowFeedback(m_SpeedFeedback, "Speed", m_Preferences.blinkMode ? "Extra distance" : "Extra speed");
        }

        void HideFeedback(List<ProxyFeedbackRequest> requests)
        {
            foreach (var request in requests)
            {
                this.RemoveFeedbackRequest(request);
            }
            requests.Clear();
        }

        void HideMainButtonFeedback()
        {
            HideFeedback(m_MainButtonFeedback);
        }

        void HideCrawlFeedback()
        {
            HideFeedback(m_CrawlFeedback);
        }

        void HideRotateFeedback()
        {
            HideFeedback(m_RotateFeedback);
        }

        void HideScaleFeedback()
        {
            HideFeedback(m_ScaleFeedback);
        }

        void HideSpeedFeedback()
        {
            HideFeedback(m_SpeedFeedback);
        }

        void HideResetScaleFeedback()
        {
            HideFeedback(m_ResetScaleFeedback);
        }

        public object OnSerializePreferences()
        {
            if (this.IsSharedUpdater(this))
            {
                // Share one preferences object across all instances
                foreach (var linkedObject in linkedObjects)
                {
                    ((LocomotionTool)linkedObject).m_Preferences = m_Preferences;
                }

                return m_Preferences;
            }

            return null;
        }

        public void OnDeserializePreferences(object obj)
        {
            if (this.IsSharedUpdater(this))
            {
                var preferences = obj as Preferences;
                if (preferences != null)
                    m_Preferences = preferences;

                // Share one preferences object across all instances
                foreach (var linkedObject in linkedObjects)
                {
                    var locomotionTool = (LocomotionTool)linkedObject;
                    locomotionTool.m_Preferences = m_Preferences;
                    locomotionTool.ShowCrawlFeedback();
                    locomotionTool.ShowMainButtonFeedback();
                }
            }
            else
            {
                // Share one preferences object across all instances
                foreach (var linkedObject in linkedObjects)
                {
                    var locomotionTool = (LocomotionTool)linkedObject;
                    var preferences = locomotionTool.m_Preferences;
                    if (preferences != null)
                        m_Preferences = preferences;
                }

                ShowCrawlFeedback();
                ShowMainButtonFeedback();
            }

            if (m_BlinkToggle)
                m_BlinkToggle.isOn = m_Preferences.blinkMode;

            if (m_FlyToggle)
                m_FlyToggle.isOn = !m_Preferences.blinkMode;
        }
    }
}
