using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.EditorXR.Core;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.Modules
{
    sealed class LockModule : ScriptableSettings<LockModule>, IModuleDependency<EditorXRMenuModule>, IActions,
        ISelectionChanged, IProvidesGameObjectLocking
    {
        class LockModuleAction : IAction, ITooltip
        {
            internal Func<bool> execute;
            public string tooltipText { get; set; }
            public Sprite icon { get; internal set; }

            public void ExecuteAction()
            {
                execute();
            }
        }

        const float k_MaxHoverTime = 2.0f;

#pragma warning disable 649
        [SerializeField]
        Sprite m_LockIcon;

        [SerializeField]
        Sprite m_UnlockIcon;
#pragma warning restore 649

        readonly LockModuleAction m_LockModuleAction = new LockModuleAction();
        public List<IAction> actions { get; private set; }

        // TODO: This should go away once the alternate menu stays open or if locking/unlocking from alternate menu goes
        // away entirely (e.g. because of HierarchyWorkspace)
        public Action<Transform, GameObject> updateAlternateMenu { private get; set; }

        GameObject m_CurrentHoverObject;
        Transform m_HoverRayOrigin;
        float m_HoverDuration;

        public void ConnectDependency(EditorXRMenuModule dependency)
        {
            updateAlternateMenu = (rayOrigin, o) => dependency.SetAlternateMenuVisibility(rayOrigin, o != null);
        }

        public void LoadModule()
        {
            m_LockModuleAction.execute = ToggleLocked;
            UpdateAction(null);

            actions = new List<IAction> { m_LockModuleAction };
        }

        public void UnloadModule() { }

        public bool IsLocked(GameObject go)
        {
            if (!go)
                return false;

            // EditorVR objects (i.e. PlayerHead) may get HideAndDontSave, which includes NotEditable, but should not count as locked
            var moduleParent = ModuleLoaderCore.instance.GetModuleParent();
            if (go.transform.IsChildOf(moduleParent.transform))
                return false;

            return (go.hideFlags & HideFlags.NotEditable) != 0;
        }

        bool ToggleLocked()
        {
            var go = Selection.activeGameObject ?? m_CurrentHoverObject;
            var newLockState = !IsLocked(go);
            SetLocked(go, newLockState);
            return newLockState;
        }

        public void SetLocked(GameObject go, bool locked)
        {
            if (!go)
                return;

            if (locked)
            {
                go.hideFlags |= HideFlags.NotEditable;

                // You shouldn't be able to keep an object selected if you are locking it
                Selection.objects = Selection.objects.Where(o => o != go).ToArray();
            }
            else
            {
                go.hideFlags &= ~HideFlags.NotEditable;
            }

            UpdateAction(go);
        }

        void UpdateAction(GameObject go)
        {
            var isLocked = IsLocked(go);
            m_LockModuleAction.tooltipText = isLocked ? "Unlock" : "Lock";
            m_LockModuleAction.icon = isLocked ? m_LockIcon : m_UnlockIcon;
        }

        public void OnHovered(GameObject go, Transform rayOrigin)
        {
            // Latch a new ray origin only when nothing is being hovered over
            if (Selection.activeGameObject || !m_HoverRayOrigin)
            {
                m_HoverRayOrigin = rayOrigin;
                m_CurrentHoverObject = go;
                m_HoverDuration = 0f;
            }
            else if (m_HoverRayOrigin == rayOrigin)
            {
                if (!go) // Ray origin is no longer hovering over any object
                {
                    // Turn off menu if it was previously shown
                    if (IsLocked(m_CurrentHoverObject))
                        updateAlternateMenu(rayOrigin, null);

                    m_HoverRayOrigin = null;
                    m_CurrentHoverObject = null;
                }
                else if (m_CurrentHoverObject == go) // Keep track of existing hover object
                {
                    m_HoverDuration += Time.deltaTime;

                    // Don't allow hover menu if over a selected game object
                    if (IsLocked(go) && m_HoverDuration >= k_MaxHoverTime)
                    {
                        UpdateAction(go);

                        // Open up the menu, so that locking can be changed
                        updateAlternateMenu(rayOrigin, go);
                    }
                }
                else // Switch to new hover object on the same ray origin
                {
                    // Turn off menu if it was previously shown
                    if (IsLocked(m_CurrentHoverObject))
                        updateAlternateMenu(rayOrigin, null);

                    m_CurrentHoverObject = go;
                    m_HoverDuration = 0f;
                }
            }
        }

        public void OnSelectionChanged()
        {
            UpdateAction(Selection.activeGameObject);
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var lockingSubscriber = obj as IFunctionalitySubscriber<IProvidesGameObjectLocking>;
            if (lockingSubscriber != null)
                lockingSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
