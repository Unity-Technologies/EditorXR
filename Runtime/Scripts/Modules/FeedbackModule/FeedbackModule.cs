using System;
using System.Collections.Generic;
using TMPro;
using Unity.EditorXR.Core;
using Unity.EditorXR.Handles;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.UI;
using Unity.EditorXR.Interfaces;
using Unity.XRTools.ModuleLoader;

namespace Unity.EditorXR.Modules
{
    class FeedbackModule : ScriptableSettings<FeedbackModule>, IDelayedInitializationModule, ISettingsMenuItemProvider,
        ISerializePreferences, IInterfaceConnector, IProvidesRequestFeedback
    {
        [Serializable]
        class Preferences
        {
            [SerializeField]
            bool m_Enabled = true;

            public bool enabled
            {
                get { return m_Enabled; }
                set { m_Enabled = value; }
            }
        }

#pragma warning disable 649
        [SerializeField]
        GameObject m_SettingsMenuItemPrefab;
#pragma warning restore 649

        readonly List<Toggle> m_Toggles = new List<Toggle>();

        Preferences m_Preferences;

        readonly List<IFeedbackReceiver> m_FeedbackReceivers = new List<IFeedbackReceiver>();
        readonly Dictionary<Type, Queue<FeedbackRequest>> m_FeedbackRequestPool = new Dictionary<Type, Queue<FeedbackRequest>>();

        public GameObject settingsMenuItemPrefab { get { return m_SettingsMenuItemPrefab; } }

        public GameObject settingsMenuItemInstance
        {
            set
            {
                if (value == null)
                    return;

                var toggle = value.GetComponent<Toggle>();
                if (m_Preferences != null)
                    toggle.isOn = m_Preferences.enabled;

                m_Toggles.Add(toggle);

                var label = value.GetComponentInChildren<TextMeshProUGUI>();

                const string feedbackEnabled = "Feedback enabled";
                const string feedbackDisabled = "Feedback disabled";
                const string enableFeedback = "Enable feedback";
                const string disableFeedback = "Disable feedback";

                toggle.onValueChanged.AddListener(isOn =>
                {
                    label.text = isOn ? feedbackEnabled : feedbackDisabled;
                    SetEnabled(isOn);
                });

                var handle = value.GetComponent<BaseHandle>();
                handle.hoverStarted += (baseHandle, data) => { label.text = m_Preferences.enabled ? disableFeedback : enableFeedback; };
                handle.hoverEnded += (baseHandle, data) => { label.text = m_Preferences.enabled ? feedbackEnabled : feedbackDisabled; };
            }
        }

        public Transform rayOrigin { get { return null; } }

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

        public void LoadModule()
        {
            if (m_Preferences == null)
                m_Preferences = new Preferences();
        }

        public void Initialize()
        {
            m_Toggles.Clear();
            m_FeedbackReceivers.Clear();
            m_FeedbackRequestPool.Clear();
        }

        public void Shutdown() { }

        public void UnloadModule() { }

        void AddReceiver(IFeedbackReceiver feedbackReceiver)
        {
            m_FeedbackReceivers.Add(feedbackReceiver);
        }

        void RemoveReceiver(IFeedbackReceiver feedbackReceiver)
        {
            m_FeedbackReceivers.Remove(feedbackReceiver);
        }

        void SetEnabled(bool enabled)
        {
            if (m_Preferences.enabled != enabled)
            {
                m_Preferences.enabled = enabled;
                if (!enabled)
                {
                    foreach (var receiver in m_FeedbackReceivers)
                    {
                        receiver.ClearFeedbackRequests(null);
                    }
                }
            }
        }

        public void AddFeedbackRequest(FeedbackRequest request)
        {
            if (!m_Preferences.enabled)
                return;

            foreach (var receiver in m_FeedbackReceivers)
            {
                receiver.AddFeedbackRequest(request);
            }
        }

        public void RemoveFeedbackRequest(FeedbackRequest request)
        {
            foreach (var receiver in m_FeedbackReceivers)
            {
                receiver.RemoveFeedbackRequest(request);
            }

            RecycleFeedbackRequestObject(request);
        }

        public void ClearFeedbackRequests(IUsesRequestFeedback caller)
        {
            if (caller == null) // Requesters are not allowed to clear all requests
                return;

            foreach (var receiver in m_FeedbackReceivers)
            {
                receiver.ClearFeedbackRequests(caller);
            }
        }

        public TRequest GetFeedbackRequestObject<TRequest>(IUsesRequestFeedback caller)
            where TRequest : FeedbackRequest, new()
        {
            Queue<FeedbackRequest> pool;
            if (!m_FeedbackRequestPool.TryGetValue(typeof(TRequest), out pool))
            {
                pool = new Queue<FeedbackRequest>();
                m_FeedbackRequestPool[typeof(TRequest)] = pool;
            }

            if (pool.Count > 0)
            {
                var request = pool.Dequeue();
                request.Reset();
                request.caller = caller;
                return (TRequest)request;
            }

            return new TRequest{ caller = caller };
        }

        void RecycleFeedbackRequestObject(FeedbackRequest request)
        {
            var type = request.GetType();
            Queue<FeedbackRequest> pool;
            if (!m_FeedbackRequestPool.TryGetValue(type, out pool))
            {
                pool = new Queue<FeedbackRequest>();
                m_FeedbackRequestPool[type] = pool;
            }

            pool.Enqueue(request);
        }

        public object OnSerializePreferences()
        {
            return m_Preferences;
        }

        public void OnDeserializePreferences(object obj)
        {
            var preferences = obj as Preferences;
            if (preferences != null)
                m_Preferences = preferences;

            foreach (var toggle in m_Toggles)
            {
                toggle.isOn = m_Preferences.enabled;
            }
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var serializePreferences = target as IFeedbackReceiver;
            if (serializePreferences != null)
                AddReceiver(serializePreferences);
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var serializePreferences = target as IFeedbackReceiver;
            if (serializePreferences != null)
                RemoveReceiver(serializePreferences);
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var feedbackRequestSubscriber = obj as IFunctionalitySubscriber<IProvidesRequestFeedback>;
            if (feedbackRequestSubscriber != null)
                feedbackRequestSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
