using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Labs.Utils;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class TooltipsEditor : EditorWindow
    {
        readonly Dictionary<Type, ITooltip> m_TooltipAttributes = new Dictionary<Type, ITooltip>();
        readonly List<Type> m_TooltipClasses = new List<Type>();
        readonly Dictionary<ITooltip, GameObject> m_TooltipsInPrefabs = new Dictionary<ITooltip, GameObject>();

        Vector2 m_Scroll;
        GUIStyle m_ButtonStyle;

        [MenuItem("Edit/Project Settings/EditorXR/Tooltips")]
        static void Init()
        {
            GetWindow<TooltipsEditor>("Tooltip Editor").Show();
        }

        void OnEnable()
        {
            m_ButtonStyle = new GUIStyle(EditorStyles.miniButton);
            m_ButtonStyle.alignment = TextAnchor.MiddleLeft;
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

            m_TooltipClasses.Clear();
            typeof(ITooltip).GetImplementationsOfInterface(m_TooltipClasses);
        }

        void CollectTooltipAttributes(Type type)
        {
            var tooltips = type.GetCustomAttributes(typeof(ITooltip), true);
            foreach (ITooltip tooltip in tooltips)
            {
                m_TooltipAttributes[type] = tooltip;
            }
        }

        void OnGUI()
        {
            if (Event.current.Equals(Event.KeyboardEvent("^w")))
            {
                Close();
                GUIUtility.ExitGUI();
            }

            const float columnWidth = 250f;
            EditorGUIUtility.labelWidth = columnWidth;

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);
            GUILayout.Label("Tooltips in Prefabs", EditorStyles.boldLabel);
            foreach (var kvp in m_TooltipsInPrefabs)
            {
                GUILayout.BeginHorizontal();

                var tooltip = kvp.Key;
                var prefab = kvp.Value;
                var mb = (MonoBehaviour)tooltip;

                var label = string.Format("{0}/{1}", prefab.name, mb.name);
                if (GUILayout.Button(label, m_ButtonStyle, GUILayout.Width(columnWidth)))
                    EditorGUIUtility.PingObject(prefab);

                try
                {
                    var textProperty = tooltip.GetType().GetProperty("tooltipText");

                    var setter = textProperty.GetSetMethod(true);
                    if (setter != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        setter.Invoke(tooltip, new object[]
                        {
                            GUILayout.TextField(tooltip.tooltipText)
                        });

                        if (EditorGUI.EndChangeCheck())
                            EditorUtility.SetDirty(prefab);
                    }
                    else
                    {
                        GUILayout.Label(tooltip.tooltipText);
                    }
                }
                catch
                {
                    GUILayout.Label("Dynamic Text");
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Tooltip Attributes", EditorStyles.boldLabel);
            foreach (var kvp in m_TooltipAttributes)
            {
                EditorGUILayout.LabelField(kvp.Key.Name, kvp.Value.tooltipText);
            }

            EditorGUILayout.Separator();

            GUILayout.Label("ITooltip Implementers", EditorStyles.boldLabel);
            foreach (var tooltipClass in m_TooltipClasses)
            {
                EditorGUILayout.LabelField(tooltipClass.Name);
            }

            GUILayout.EndScrollView();
        }
    }
}
