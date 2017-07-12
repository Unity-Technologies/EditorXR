using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
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

		[MenuItem("Edit/Project Settings/EditorVR/Haptic Pulses")]
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
			const float nameColumnWidth = 250f;
			const float durationColumnWidth = 60f;

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
			GUILayout.Label("Duration", GUILayout.Width(durationColumnWidth));
			GUILayout.Label("Intensity");
			GUILayout.EndHorizontal();

			foreach (var pulse in m_HapticPulses)
			{
				GUILayout.BeginHorizontal();
				EditorGUILayout.ObjectField(pulse.pulse, typeof(HapticPulse), false, GUILayout.Width(nameColumnWidth));
				EditorGUI.BeginChangeCheck();
				pulse.duration.floatValue = EditorGUILayout.FloatField(pulse.duration.floatValue, GUILayout.Width(durationColumnWidth));
				pulse.intensity.floatValue = GUILayout.HorizontalSlider(pulse.intensity.floatValue, 0, 1);
				pulse.intensity.floatValue = EditorGUILayout.FloatField(pulse.intensity.floatValue, GUILayout.Width(durationColumnWidth));
				if (EditorGUI.EndChangeCheck())
				{
					pulse.serializedObject.ApplyModifiedProperties();
				}

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}

		void OnUndoRedo()
		{
			Reset();
			Repaint();
		}
	}
}
