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

		[Serializable]
		class SerializedPreferences
		{
			public List<SerializedPreferenceItem> items = new List<SerializedPreferenceItem>();
		}

		[Serializable]
		class SerializedPreferenceItem
		{
			public string name;
			public string payloadType;
			public string payload;
		}

		public void ConnectInterface(object obj, Transform rayOrigin = null)
		{
			var serializer = obj as ISerializePreferences;
			if (serializer != null)
				m_Serializers.Add(serializer);
		}

		public void DisconnectInterface(object obj)
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
			foreach (var serializer in m_Serializers)
			{
				var payload = serializer.OnSerializePreferences();

				var item = new SerializedPreferenceItem();
				item.name = serializer.GetType().FullName;
				item.payloadType = payload.GetType().FullName;
				item.payload = JsonUtility.ToJson(payload);
				preferences.items.Add(item);
			}

			return JsonUtility.ToJson(preferences);
		}
	}
}
#endif
