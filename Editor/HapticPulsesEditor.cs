#if ENABLE_EDITORXR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class HapticPulseEditor : EditorWindow
    {
        class Pulse
        {
            public HapticPulse pulse;
            public SerializedObject serializedObject;
            public SerializedProperty duration;
            public SerializedProperty intensity;
        }

        readonly List<Pulse> m_HapticPulses = new List<Pulse>();

        Vector2 m_Scroll;
        float m_Multiplier = 1;

        [MenuItem("Edit/Project Settings/EditorXR/Haptic Pulses")]
        static void Init()
        {
            GetWindow<HapticPulseEditor>("Haptic Pulse Editor").Show();
        }

        void OnEnable()
        {
            Reset();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void Reset()
        {
            m_HapticPulses.Clear();
            var pulses = AssetDatabase.FindAssets("t:HapticPulse");
            foreach (var guid in pulses)
            {
                var pulse = AssetDatabase.LoadAssetAtPath<HapticPulse>(AssetDatabase.GUIDToAssetPath(guid));
                if (pulse)
                {
                    var serializedObject = new SerializedObject(pulse);
                    m_HapticPulses.Add(new Pulse
                    {
                        pulse = pulse,
                        serializedObject = serializedObject,
                        duration = serializedObject.FindProperty("m_Duration"),
                        intensity = serializedObject.FindProperty("m_Intensity")
                    });
                }
            }
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnGUI()
        {
            if (Event.current.Equals(Event.KeyboardEvent("^w")))
            {
                Close();
                GUIUtility.ExitGUI();
            }

            const float nameColumnWidth = 250f;
            const float floatFieldColumnWidth = 60f;

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);

            GUILayout.BeginHorizontal();
            m_Multiplier = EditorGUILayout.FloatField("Multiplier", m_Multiplier);
            if (GUILayout.Button("Multiply Intensity"))
            {
                foreach (var pulse in m_HapticPulses)
                {
                    pulse.intensity.floatValue *= m_Multiplier;
                    pulse.serializedObject.ApplyModifiedProperties();
                }
            }

            if (GUILayout.Button("Multiply Duration"))
            {
                foreach (var pulse in m_HapticPulses)
                {
                    pulse.duration.floatValue *= m_Multiplier;
                    pulse.serializedObject.ApplyModifiedProperties();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Haptic Pulses", EditorStyles.boldLabel, GUILayout.Width(nameColumnWidth));
            GUILayout.Label("Duration");
            GUILayout.Label("Intensity");
            GUILayout.EndHorizontal();

            foreach (var pulse in m_HapticPulses)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(pulse.pulse, typeof(HapticPulse), false, GUILayout.Width(nameColumnWidth));
                EditorGUI.BeginChangeCheck();
                var durationProperty = pulse.duration;
                durationProperty.floatValue = GUILayout.HorizontalSlider(durationProperty.floatValue, 0, HapticsModule.MaxDuration);
                durationProperty.floatValue = EditorGUILayout.FloatField(durationProperty.floatValue, GUILayout.Width(floatFieldColumnWidth));
                durationProperty.floatValue = Mathf.Clamp(durationProperty.floatValue, 0, HapticsModule.MaxDuration);
                var intensityProperty = pulse.intensity;
                intensityProperty.floatValue = GUILayout.HorizontalSlider(intensityProperty.floatValue, 0, 1);
                intensityProperty.floatValue = EditorGUILayout.FloatField(intensityProperty.floatValue, GUILayout.Width(floatFieldColumnWidth));
                intensityProperty.floatValue = Mathf.Clamp01(intensityProperty.floatValue);
                if (EditorGUI.EndChangeCheck())
                    pulse.serializedObject.ApplyModifiedProperties();

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        void OnUndoRedo()
        {
            Reset();
            Repaint();
        }

        void OnProjectChange()
        {
            Reset();
            Repaint();
        }
    }
}
#endif
