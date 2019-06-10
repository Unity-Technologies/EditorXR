using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class UndoMenu : MonoBehaviour, IInstantiateUI, IUsesMenuOrigins, ICustomActionMap,
        IControlHaptics, IUsesNode, IConnectInterfaces, IUsesRequestFeedback, IUsesDeviceType, IAlternateMenu
    {
        const float k_UndoRedoThreshold = 0.5f;
        const string k_EngageControlName = "Engage";
        const float k_EngageUndoAfterStickReleasedDuration = 0.1f; // Duration after releasing the joystick to still accept a left/right flick to undo/redo.
        const string k_FeedbackHintForJoystickController = "Click + flick left/right to undo/redo";
        const string k_FeedbackHintForTrackpadController = "Click left/right side to undo/redo";

#pragma warning disable 649
        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        UndoMenuUI m_UndoMenuPrefab;

        [SerializeField]
        HapticPulse m_UndoPulse;
#pragma warning restore 649

        UndoMenuUI m_UndoMenuUI;
        Transform m_AlternateMenuOrigin;
        MenuHideFlags m_MenuHideFlags = MenuHideFlags.Hidden;
        float m_PrevNavigateX;
        bool m_StillEngagedAfterStickRelease;
        Coroutine m_StillEngagedAfterStickReleasedCoroutine;
        bool m_TrackpadController;
        string m_FeedbackHintForCurrentController;

        readonly BindingDictionary m_Controls = new BindingDictionary();

        public Transform menuOrigin { get; set; }
        public Transform rayOrigin { get; set; }
        public Node node { get; set; }

        public GameObject menuContent { get { return m_UndoMenuUI.gameObject; } }
        public Bounds localBounds { get { return default(Bounds); } }
        public int priority { get { return 0; } }
        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get { return false; } }

        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                m_AlternateMenuOrigin = value;

                if (m_UndoMenuUI != null)
                    m_UndoMenuUI.alternateMenuOrigin = value;
            }
        }

        public MenuHideFlags menuHideFlags
        {
            get { return m_MenuHideFlags; }
            set
            {
                if (m_MenuHideFlags != value)
                {
                    m_MenuHideFlags = value;

                    var visible = value == 0;
                    if (m_UndoMenuUI)
                        m_UndoMenuUI.visible = visible;

                    if (visible)
                        ShowFeedback();
                }
            }
        }

#if !FI_AUTOFILL
        IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
#endif

        void Start()
        {
            m_UndoMenuUI = this.InstantiateUI(m_UndoMenuPrefab.gameObject).GetComponent<UndoMenuUI>();
            m_UndoMenuUI.alternateMenuOrigin = alternateMenuOrigin;
            this.ConnectInterfaces(m_UndoMenuUI); // Connect interfaces before performing setup on the UI
            m_UndoMenuUI.gameObject.SetActive(false);
            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
            m_TrackpadController = this.GetDeviceType() == DeviceType.Vive;
            m_FeedbackHintForCurrentController = m_TrackpadController
                ? k_FeedbackHintForTrackpadController
                : k_FeedbackHintForJoystickController;
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            if (!m_UndoMenuUI.visible)
                return;

            var undoMenuInput = (UndoMenuInput)input;
            if (undoMenuInput == null)
            {
                this.ClearFeedbackRequests(this);
                return;
            }

            var engage = undoMenuInput.engage;
            if (engage.wasJustReleased && !m_TrackpadController)
                this.RestartCoroutine(ref m_StillEngagedAfterStickReleasedCoroutine, AcceptInputAfterStickReleased());

            if (!(engage.wasJustPressed || !m_TrackpadController && (engage.isHeld || m_StillEngagedAfterStickRelease)))
                return;

            if (!m_TrackpadController)
                consumeControl(engage);

            m_UndoMenuUI.engaged = true;

            var navigateXControl = undoMenuInput.navigateX;
            var navigateX = navigateXControl.value;
            var undoRedoPerformed = false;
            if (navigateX < -k_UndoRedoThreshold && (m_TrackpadController || m_PrevNavigateX > -k_UndoRedoThreshold))
            {
#if UNITY_EDITOR
                Undo.PerformUndo();
#endif
                m_UndoMenuUI.StartPerformedAnimation(true);
                ShowUndoPerformedFeedback(true);
                undoRedoPerformed = true;
            }
            else if (navigateX > k_UndoRedoThreshold && (m_TrackpadController || m_PrevNavigateX < k_UndoRedoThreshold))
            {
#if UNITY_EDITOR
                Undo.PerformRedo();
#endif
                m_UndoMenuUI.StartPerformedAnimation(false);
                ShowUndoPerformedFeedback(false);
                undoRedoPerformed = true;
            }

            m_PrevNavigateX = navigateX;

            if (undoRedoPerformed)
            {
                consumeControl(navigateXControl);
                this.StopCoroutine(ref m_StillEngagedAfterStickReleasedCoroutine);
                this.Pulse(node, m_UndoPulse);
            }
        }

        IEnumerator AcceptInputAfterStickReleased()
        {
            m_StillEngagedAfterStickRelease = true;
            yield return new WaitForSeconds(k_EngageUndoAfterStickReleasedDuration);
            m_StillEngagedAfterStickRelease = false;
            m_UndoMenuUI.engaged = false;
        }

        void ShowFeedback()
        {
            List<VRInputDevice.VRControl> controls;
            if (m_Controls.TryGetValue(k_EngageControlName, out controls))
            {
                foreach (var id in controls)
                {
                    var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);;
                    request.control = id;
                    request.node = node;
                    request.tooltipText = m_FeedbackHintForCurrentController;
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowUndoPerformedFeedback(bool undo)
        {
            List<VRInputDevice.VRControl> controls;
            if (m_Controls.TryGetValue(k_EngageControlName, out controls))
            {
                foreach (var id in controls)
                {
                    var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);;
                    request.control = id;
                    request.node = node;
                    request.tooltipText = string.Format("{0} performed", undo ? "Undo" : "Redo");
                    this.AddFeedbackRequest(request);
                }
            }
        }
    }
}
