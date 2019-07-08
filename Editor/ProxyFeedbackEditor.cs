#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class ProxyFeedbackEditor : EditorWindow
    {
        readonly Dictionary<Type, SerializedProxyFeedback> m_SerializedFeedback = new Dictionary<Type, SerializedProxyFeedback>();
        Vector2 m_Scroll;

        [MenuItem("Edit/Project Settings/EditorXR/Proxy Feedback")]
        static void Init()
        {
            GetWindow<ProxyFeedbackEditor>("Proxy Feedback Editor").Show();
        }

        void OnEnable()
        {
            Refresh();
        }

        void OnGUI()
        {
            if (Event.current.Equals(Event.KeyboardEvent("^w")))
            {
                Close();
                GUIUtility.ExitGUI();
            }

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
            var preferences = SerializedPreferencesModule.DeserializePreferences();
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
            var preferences = SerializedPreferencesModule.DeserializePreferences();
            if (preferences == null)
                return;

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
            var preferences = SerializedPreferencesModule.DeserializePreferences();
            if (preferences == null)
                return;

            var items = preferences.items;
            foreach (var kvp in m_SerializedFeedback)
            {
                items[kvp.Key].payload = JsonUtility.ToJson(kvp.Value);
            }

            SerializedPreferencesModule.serializedPreferences = JsonUtility.ToJson(preferences);
        }
    }
}
#endif
