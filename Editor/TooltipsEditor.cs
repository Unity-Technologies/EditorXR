using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Utilities;
using TooltipAttribute = UnityEngine.Experimental.EditorVR.TooltipAttribute;

public class TooltipsEditor : EditorWindow
{
	readonly Dictionary<Type, TooltipAttribute> m_TooltipAttributes = new Dictionary<Type, TooltipAttribute>();
	IEnumerable<Type> m_TooltipClasses;
	readonly Dictionary<ITooltip, GameObject> m_TooltipsInPrefabs = new Dictionary<ITooltip, GameObject>();

	Vector2 m_Scroll;

	[MenuItem("Window/Tooltips")]
	static void Init()
	{
		((EditorWindow) GetWindow<TooltipsEditor>()).Show();
	}

	void OnEnable()
	{
		m_TooltipsInPrefabs.Clear();
		foreach (var path in AssetDatabase.GetAllAssetPaths())
		{
			if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(GameObject))
			{
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				foreach (var tooltip in asset.GetComponentsInChildren<ITooltip>(true))
				{
					m_TooltipsInPrefabs[tooltip] = asset;
				}
			}
		}

		m_TooltipAttributes.Clear();
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (var assembly in assemblies)
		{
			foreach (var type in assembly.GetTypes())
			{
				CollectTooltipAttributes(type);
				foreach (var nestedType in type.GetNestedTypes(BindingFlags.NonPublic))
				{
					CollectTooltipAttributes(nestedType);
				}
			}
		}

		m_TooltipClasses = U.Object.GetImplementationsOfInterface(typeof(ITooltip));
	}

	void CollectTooltipAttributes(Type type)
	{
		var tooltips = type.GetCustomAttributes(typeof(TooltipAttribute), true);
		foreach (TooltipAttribute tooltip in tooltips)
		{
			m_TooltipAttributes[type] = tooltip;
		}
	}

	void OnGUI()
	{
		EditorGUIUtility.labelWidth = 250;
		m_Scroll = GUILayout.BeginScrollView(m_Scroll);
		GUILayout.Label("Tooltips in prefabs");
		foreach (var kvp in m_TooltipsInPrefabs)
		{
			GUILayout.BeginHorizontal();
			var tooltip = kvp.Key;
			var prefab = kvp.Value;
			var mb = (MonoBehaviour)tooltip;
			var label = string.Format("{0}/{1}", prefab.name, mb.name);
			try
			{
				var textProperty = tooltip.GetType().GetProperty("tooltipText");
				var setter = textProperty.GetSetMethod(true);
				if (setter != null)
				{
					EditorGUI.BeginChangeCheck();
					setter.Invoke(tooltip, new object[]
					{
						EditorGUILayout.TextField(label, tooltip.tooltipText)
					});
					if (EditorGUI.EndChangeCheck())
						EditorUtility.SetDirty(prefab);
				}
				else
				{
					EditorGUILayout.LabelField(label, tooltip.tooltipText);
				}
			}
			catch
			{
				EditorGUILayout.LabelField(label, "Dynamic Text");
			}

			if (GUILayout.Button("Ping", GUILayout.Width(50)))
			{
				EditorGUIUtility.PingObject(prefab);
			}
			GUILayout.EndHorizontal();
		}

		EditorGUILayout.Separator();

		GUILayout.Label("Tooltip Attributes");
		foreach (var kvp in m_TooltipAttributes)
		{
			EditorGUILayout.LabelField(kvp.Key.Name, kvp.Value.text);
		}

		EditorGUILayout.Separator();

		GUILayout.Label("ITooltip Implementers");
		foreach (var tooltipClass in m_TooltipClasses)
		{
			EditorGUILayout.LabelField(tooltipClass.Name);
		}

		GUILayout.EndScrollView();
	}
}
