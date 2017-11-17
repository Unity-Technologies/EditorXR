#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class UndoMenu : MonoBehaviour, IInstantiateUI, IAlternateMenu, IUsesMenuOrigins, ICustomActionMap,
        IControlHaptics, IUsesNode, IConnectInterfaces, IRequestFeedback
    {
        const float k_UndoRedoThreshold = 0.5f;
        const float k_EngageUndoAfterStickReleasedDuration = 0.1f; // Duration after releasing the joystick to still accept a left/right flick to undo/redo.

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        UndoMenuUI m_UndoMenuPrefab;

        [SerializeField]
        HapticPulse m_UndoPulse;

        UndoMenuUI m_UndoMenuUI;
        List<ActionMenuData> m_MenuActions;
        Transform m_AlternateMenuOrigin;
        MenuHideFlags m_MenuHideFlags = MenuHideFlags.Hidden;
        float m_PrevNavigateX;
        bool m_StillEngagedAfterStickRelease;
        Coroutine m_StillEngagedAfterStickReleasedCoroutine;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        
        public Transform rayOrigin { private get; set; }

        public Transform menuOrigin { get; set; }

        public GameObject menuContent { get { return m_UndoMenuUI.gameObject; } }

        public Node node { get; set; }

        public event Action<Transform> itemWasSelected;

        public Bounds localBounds { get { return default(Bounds); } }

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreLocking { get { return false; } }

        public List<ActionMenuData> menuActions
        {
            get { return m_MenuActions; }
            set
            {
                m_MenuActions = value;

                if (m_UndoMenuUI)
                    m_UndoMenuUI.actions = value;
            }
        }

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
                    else
                        this.ClearFeedbackRequests();
                }
            }
        }

        void Start()
        {
            m_UndoMenuUI = this.InstantiateUI(m_UndoMenuPrefab.gameObject).GetComponent<UndoMenuUI>();
            m_UndoMenuUI.alternateMenuOrigin = alternateMenuOrigin;
            m_UndoMenuUI.actions = menuActions;
            this.ConnectInterfaces(m_UndoMenuUI); // Connect interfaces before performing setup on the UI
            m_UndoMenuUI.Setup();
            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            var undoMenuInput = (UndoMenuInput)input;
            if (undoMenuInput == null)
            {
                this.ClearFeedbackRequests();
                return;
            }
            if (undoMenuInput.engage.wasJustReleased)
                this.RestartCoroutine(ref m_StillEngagedAfterStickReleasedCoroutine, AcceptInputAfterStickReleased());
            if (!(undoMenuInput.engage.wasJustPressed || undoMenuInput.engage.isHeld || m_StillEngagedAfterStickRelease))
                return;
            consumeControl(undoMenuInput.engage);
            m_UndoMenuUI.engaged = true;

            var navigateX = undoMenuInput.navigateX.value;
            var undoRedoEngaged = false;
            if (navigateX < -k_UndoRedoThreshold && m_PrevNavigateX > -k_UndoRedoThreshold)
            {
                Undo.PerformUndo();
                undoRedoEngaged = true;
            }
            else if (navigateX > k_UndoRedoThreshold && m_PrevNavigateX < k_UndoRedoThreshold)
            {
                Undo.PerformRedo();
                undoRedoEngaged = true;
            }
            m_PrevNavigateX = navigateX;
            if (undoRedoEngaged)
            {
                consumeControl(undoMenuInput.navigateX);
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
            if (m_Controls.TryGetValue("SelectItem", out controls))
            {
                foreach (var id in controls)
                {
                    this.AddFeedbackRequest(new ProxyFeedbackRequest
                    {
                        control = id,
                        node = node,
                        tooltipText = "Select Action (Press to Execute)"
                    });
                }
            }
        }
    }
}
#endif
