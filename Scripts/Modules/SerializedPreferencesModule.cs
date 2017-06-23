#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SerializedPreferencesModule : MonoBehaviour, IInterfaceConnector
	{
		List<ISerializePreferences> m_Serializers = new List<ISerializePreferences>();
		SerializedPreferences m_Preferences;

		[Serializable]
		class SerializedPreferences
		{
			[SerializeField]
			List<SerializedPreferenceItem> m_Items = new List<SerializedPreferenceItem>();

			public List<SerializedPreferenceItem> items { get { return m_Items; } }
		}

		[Serializable]
		class SerializedPreferenceItem
		{
			[SerializeField]
			string m_Name;
			[SerializeField]
			string m_PayloadType;
			[SerializeField]
			string m_Payload;

			public string name { get { return m_Name; } set { m_Name = value; } }
			public string payloadType { get { return m_PayloadType; } set { m_PayloadType = value; } }
			public string payload { get { return m_Payload; } set { m_Payload = value; } }
		}

		public void ConnectInterface(object obj, Transform rayOrigin = null)
		{
			var serializer = obj as ISerializePreferences;
			if (serializer != null)
				m_Serializers.Add(serializer);
		}

		public void DisconnectInterface(object obj, Transform rayOrigin = null)
		{
			var serializer = obj as ISerializePreferences;
			if (serializer != null)
				m_Serializers.Remove(serializer);
		}

		internal void DeserializePreferences(string serializedPreferences)
		{
			var preferences = JsonUtility.FromJson<SerializedPreferences>(serializedPreferences);
			if (preferences != null)
			{
				m_Preferences = preferences;

				foreach (var serializer in m_Serializers)
				{
					var item = preferences.items.SingleOrDefault(pi => pi.name == serializer.GetType().FullName);
					if (item != null)
					{
						var payload = JsonUtility.FromJson(item.payload, Type.GetType(item.payloadType));
						serializer.OnDeserializePreferences(payload);
					}
				}
			}
		}

		internal string SerializePreferences()
		{
			var preferences = new SerializedPreferences();
			var payloads = new Dictionary<Type, SerializedPreferenceItem>();

			// Use the previously saved preferences as defaults
			if (m_Preferences != null)
			{
				foreach (var serializer in m_Serializers)
				{
					var type = serializer.GetType();
					payloads[type] = m_Preferences.items.SingleOrDefault(pi => pi.name == type.FullName);
				}
			}

			foreach (var serializer in m_Serializers)
			{
				var payload = serializer.OnSerializePreferences();

				if (payload == null)
					continue;

				var type = serializer.GetType();
				payloads[type] = new SerializedPreferenceItem
				{
					name = type.FullName,
					payloadType = payload.GetType().FullName,
					payload = JsonUtility.ToJson(payload)
				};
			}

			preferences.items.AddRange(payloads.Values);
			m_Preferences = preferences;

			return JsonUtility.ToJson(preferences);
		}
	}
}
#endif
