using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.EditorVR.UI
{
    class TooltipsSettingsProvider : EditorXRSettingsProvider
    {
        const string HapticPulsesPath = k_Path + "/Tooltips";

        readonly Dictionary<Type, ITooltip> m_TooltipAttributes = new Dictionary<Type, ITooltip>();
        readonly List<Type> m_TooltipClasses = new List<Type>();
        readonly Dictionary<ITooltip, GameObject> m_TooltipsInPrefabs = new Dictionary<ITooltip, GameObject>();

        Vector2 m_Scroll;

        protected TooltipsSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        [SettingsProvider]
        public static SettingsProvider CreateHapticPulsesSettingsProvider()
        {
            var provider = new TooltipsSettingsProvider(HapticPulsesPath);
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
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

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            const float columnWidth = 250f;
            const float buttonWidth = 40;
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
                GUILayout.Label(label, GUILayout.Width(columnWidth));

                if (GUILayout.Button("Ping", GUILayout.Width(buttonWidth)))
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
