using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Modules;
using Unity.Labs.EditorXR.Proxies;
using UnityEditor;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Unity.Labs.EditorXR.UI
{
    class ProxyFeedbackSettingsProvider : EditorXRSettingsProvider
    {
        const string ProxyFeedbackPath = k_Path + "/Proxy Feedback";

        readonly Dictionary<Type, SerializedProxyFeedback> m_SerializedFeedback = new Dictionary<Type, SerializedProxyFeedback>();
        Vector2 m_Scroll;

        protected ProxyFeedbackSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        [SettingsProvider]
        public static SettingsProvider CreateHapticPulsesSettingsProvider()
        {
            var provider = new ProxyFeedbackSettingsProvider(ProxyFeedbackPath);
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            Refresh();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            if (GUILayout.Button("Reload Data"))
                Refresh();

            if (GUILayout.Button("Clear Data"))
            {
                ClearData();
                Refresh();
            }

            if (GUILayout.Button("Save Data"))
            {
                SaveData();
                Refresh();
            }

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);
            var hasFeedback = false;
            foreach (var kvp in m_SerializedFeedback)
            {
                GUILayout.Label(kvp.Key.Name, EditorStyles.boldLabel);
                DrawPreferences(kvp.Value);
                hasFeedback = true;
            }

            if (!hasFeedback)
                GUILayout.Label("No serialized feedback");

            GUILayout.EndScrollView();
        }

        static void DrawPreferences(SerializedProxyFeedback feedback)
        {
            GUILayout.Label("Left Hand");
            DrawNode(feedback.leftNode);
            GUILayout.Label("Right Hand");
            DrawNode(feedback.rightNode);
        }

        static void DrawNode(SerializedProxyNodeFeedback node)
        {
            var keys = node.keys;
            var values = node.values;
            for (var i = 0; i < keys.Length; i++)
            {
                var data = values[i];
                data.presentations = EditorGUILayout.IntField(keys[i].ToString(), data.presentations);
            }
        }

        void Refresh()
        {
            m_SerializedFeedback.Clear();

            var preferences = SerializedPreferencesModule.SerializedPreferences.Deserialize(SerializedPreferencesModule.serializedPreferences);
            if (preferences == null)
                return;

            foreach (var kvp in preferences.items)
            {
                var type = kvp.Key;
                if (typeof(TwoHandedProxyBase).IsAssignableFrom(type))
                {
                    var item = kvp.Value;
                    Type payloadType = null;
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        payloadType = assembly.GetType(item.payloadType);
                        if (payloadType != null)
                            break;
                    }

                    if (payloadType == null)
                        continue;

                    var payload = (SerializedProxyFeedback)JsonUtility.FromJson(item.payload, payloadType);
                    m_SerializedFeedback[kvp.Key] = payload;
                }
            }
        }

        static void ClearData()
        {
            var preferences = SerializedPreferencesModule.SerializedPreferences.Deserialize(SerializedPreferencesModule.serializedPreferences);
            foreach (var kvp in new Dictionary<Type, SerializedPreferencesModule.SerializedPreferenceItem>(preferences.items))
            {
                var type = kvp.Key;
                if (typeof(TwoHandedProxyBase).IsAssignableFrom(type))
                    preferences.Remove(type);
            }

            SerializedPreferencesModule.serializedPreferences = JsonUtility.ToJson(preferences);
        }

        void SaveData()
        {
            var preferences = SerializedPreferencesModule.SerializedPreferences.Deserialize(SerializedPreferencesModule.serializedPreferences);
            var items = preferences.items;
            foreach (var kvp in m_SerializedFeedback)
            {
                items[kvp.Key].payload = JsonUtility.ToJson(kvp.Value);
            }

            SerializedPreferencesModule.serializedPreferences = JsonUtility.ToJson(preferences);
        }
    }
}
