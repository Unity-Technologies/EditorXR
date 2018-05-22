
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    [Serializable]
    class SerializedProxyFeedback
    {
        public SerializedProxyNodeFeedback leftNode;
        public SerializedProxyNodeFeedback rightNode;
    }

    /// <summary>
    /// Used as globally unique identifiers for feedback requests
    /// They are used to relate feedback requests to the persistent count of visible presentations used to suppress feedback
    /// </summary>
    [Serializable]
    class RequestKey
    {
        /// <summary>
        /// The control index used to identify the related affordance
        /// </summary>
        [SerializeField]
        VRInputDevice.VRControl m_Control;

        /// <summary>
        /// The tooltip text that was presented
        /// </summary>
        [SerializeField]
        string m_TooltipText;

        public void UpdateValues(ProxyFeedbackRequest request)
        {
            m_Control = request.control;
            m_TooltipText = request.tooltipText;
        }

        public bool HasTooltip()
        {
            return !string.IsNullOrEmpty(m_TooltipText);
        }

        public override int GetHashCode()
        {
            var hashCode = (int)m_Control;

            if (m_TooltipText != null)
                hashCode ^= m_TooltipText.GetHashCode();

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is RequestKey))
                return false;

            var key = (RequestKey)obj;
            return m_Control == key.m_Control && string.Equals(m_TooltipText, key.m_TooltipText);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", m_Control, m_TooltipText);
        }
    }

    /// <summary>
    /// Contains per-request persistent data
    /// </summary>
    [Serializable]
    class RequestData
    {
        [SerializeField]
        int m_Presentations;

        readonly Action m_OnBecameVisible;

        /// <summary>
        /// How many times the user viewed the presentation of this type of request
        /// </summary>
        public int presentations
        {
            get { return m_Presentations; }
            set { m_Presentations = value; }
        }

        public bool visibleThisPresentation { get; set; }

        public Action onBecameVisible
        {
            get { return m_OnBecameVisible; }
        }

        public RequestData()
        {
            m_OnBecameVisible = OnBecameVisible;
        }

        void OnBecameVisible()
        {
            if (!visibleThisPresentation)
                presentations++;

            visibleThisPresentation = true;
        }
    }

    /// <summary>
    /// Used to store persistent data about feedback requests
    /// </summary>
    [Serializable]
    class SerializedProxyNodeFeedback
    {
        public RequestKey[] keys;
        public RequestData[] values;
    }
}

