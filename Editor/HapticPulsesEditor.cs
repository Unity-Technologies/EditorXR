using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
	sealed class HapticPulseEditor : EditorWindow
	{
		readonly List<HapticPulse> m_HapticPulses = new List<HapticPulse>();

		Vector2 m_Scroll;
		float m_Multiplier = 1;

		[MenuItem("Edit/Project Settings/EditorVR/Haptic Pulses")]
		static void Init()
		{
			GetWindow<HapticPulseEditor>("Haptic Pulse Editor").Show();
		}

		void OnEnable()
		{
			m_HapticPulses.Clear();
			var pulses = AssetDatabase.FindAssets("t:HapticPulse");
			foreach (var pulse in pulses)
			{
				var asset = AssetDatabase.LoadAssetAtPath<HapticPulse>(AssetDatabase.GUIDToAssetPath(pulse));
				if (asset)
					m_HapticPulses.Add(asset);
			}
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
					pulse.intensity *= m_Multiplier;
					EditorUtility.SetDirty(pulse);
				}
			}

			if (GUILayout.Button("Multiply Duration"))
			{
				foreach (var pulse in m_HapticPulses)
				{
					pulse.duration *= m_Multiplier;
					EditorUtility.SetDirty(pulse);
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
				EditorGUILayout.ObjectField(pulse, typeof(HapticPulse), false, GUILayout.Width(nameColumnWidth));
				EditorGUI.BeginChangeCheck();
				pulse.duration = EditorGUILayout.FloatField(pulse.duration, GUILayout.Width(durationColumnWidth));
				pulse.intensity = GUILayout.HorizontalSlider(pulse.intensity, 0, 1);
				pulse.intensity = EditorGUILayout.FloatField(pulse.intensity, GUILayout.Width(durationColumnWidth));
				if (EditorGUI.EndChangeCheck())
				{
					EditorUtility.SetDirty(pulse);
				}
				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}
	}
}
