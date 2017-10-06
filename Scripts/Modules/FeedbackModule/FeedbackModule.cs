#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR
{
	public abstract class FeedbackRequest
	{
		public IRequestFeedback caller;
		public GameObject settingsMenuItemPrefab { get; private set; }
		public GameObject settingsMenuItemInstance { get; set; }
	}

	public class FeedbackModule : MonoBehaviour, ISettingsMenuItemProvider, ISerializePreferences
	{
		[Serializable]
		class Preferences
		{
			[SerializeField]
			bool m_Enabled = true;

			public bool enabled { get { return m_Enabled; } set { m_Enabled = value; } }
		}

		[SerializeField]
		GameObject m_SettingsMenuItemPrefab;

		readonly List<Toggle> m_Toggles = new List<Toggle>();

		Preferences m_Preferences;

		readonly List<IFeedbackReceiver> m_FeedbackReceivers = new List<IFeedbackReceiver>();

		public GameObject settingsMenuItemPrefab { get { return m_SettingsMenuItemPrefab; } }

		public GameObject settingsMenuItemInstance
		{
			set
			{
				var toggle = value.GetComponent<Toggle>();
				if (m_Preferences != null)
					toggle.isOn = m_Preferences.enabled;

				m_Toggles.Add(toggle);
				var label = value.GetComponentInChildren<Text>();

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
		
		void Awake()
		{
			IRequestFeedbackMethods.addFeedbackRequest = AddFeedbackRequest;
			IRequestFeedbackMethods.removeFeedbackRequest = RemoveFeedbackRequest;
			IRequestFeedbackMethods.clearFeedbackRequests = ClearFeedbackRequests;
		}

		void Start()
		{
			if (m_Preferences == null)
				m_Preferences = new Preferences();
		}

		public void AddReceiver(IFeedbackReceiver feedbackReceiver)
		{
			m_FeedbackReceivers.Add(feedbackReceiver);
		}

		public void RemoveReceiver(IFeedbackReceiver feedbackReceiver)
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

		void AddFeedbackRequest(FeedbackRequest request)
		{
			if (!m_Preferences.enabled)
				return;

			foreach (var receiver in m_FeedbackReceivers)
			{
				receiver.AddFeedbackRequest(request);
			}
		}

		void RemoveFeedbackRequest(FeedbackRequest request)
		{
			foreach (var receiver in m_FeedbackReceivers)
			{
				receiver.RemoveFeedbackRequest(request);
			}
		}

		void ClearFeedbackRequests(IRequestFeedback caller)
		{
			if (caller == null) // Requesters are not allowed to clear all requests
				return;

			foreach (var receiver in m_FeedbackReceivers)
			{
				receiver.ClearFeedbackRequests(caller);
			}
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
	}
}
#endif
