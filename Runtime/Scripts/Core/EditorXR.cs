using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Modules;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace Unity.EditorXR.Core
{
#if UNITY_EDITOR
    [RequiresTag(VRPlayerTag)]
#endif
    [ModuleOrder(ModuleOrders.EditorXRLoadOrder)]
    sealed class EditorXR : IEditor, IModule, IUsesConnectInterfaces
    {
#if UNITY_EDITOR
        class AssemblyFilter : IFilterBuildAssemblies
        {
            static readonly string[] k_EditorXRAssemblies =
            {
                "Library/ScriptAssemblies/Unity.EditorXR.dll",
                "Library/ScriptAssemblies/Unity.EditorXR.Interfaces.dll",
                "Library/ScriptAssemblies/input-prototype.dll"
            };

            public int callbackOrder { get { return 0; } }

            public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                if (includeInBuilds)
                    return assemblies;

                var assemblySet = new HashSet<string>(assemblies);
                assemblySet.ExceptWith(k_EditorXRAssemblies);
                return assemblySet.ToArray();
            }
        }
#endif

        const HideFlags k_DefaultHideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        internal const string VRPlayerTag = "VRPlayer";
        const string k_PreserveLayout = "EditorXR.PreserveLayout";
        const string k_IncludeInBuilds = "EditorXR.IncludeInBuilds";

        static bool s_IsInitialized;

        internal static bool preserveLayout
        {
            get { return EditorPrefs.GetBool(k_PreserveLayout, true); }
            set { EditorPrefs.SetBool(k_PreserveLayout, value); }
        }

        internal static bool includeInBuilds
        {
            get { return EditorPrefs.GetBool(k_IncludeInBuilds, false); }
            set { EditorPrefs.SetBool(k_IncludeInBuilds, value); }
        }

        internal static Type[] DefaultTools { get; set; }
        internal static Type DefaultMenu { get; set; }
        internal static Type DefaultAlternateMenu { get; set; }
        internal static Type[] HiddenTypes { get; set; }
        internal static Action UpdateInputManager { private get; set; }

#if !FI_AUTOFILL
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        internal static void ResetPreferences()
        {
#if UNITY_EDITOR
            EditorPrefs.DeleteKey(k_PreserveLayout);
            EditorPrefs.DeleteKey(k_IncludeInBuilds);
            EditorPrefs.DeleteKey(SerializedPreferencesModule.SerializedPreferencesKey);
            ModuleLoaderDebugSettings.instance.SetModuleHideFlags(k_DefaultHideFlags);
#endif
        }

        // Code from the previous static constructor moved here to allow for testability
        static void HandleInitialization()
        {
            if (!s_IsInitialized)
            {
                s_IsInitialized = true;

#if UNITY_EDITOR && !UNITY_2020_1_OR_NEWER
#pragma warning disable 618
                if (!PlayerSettings.GetVirtualRealitySupported(BuildTargetGroup.Standalone))
                    Debug.Log("<color=orange>EditorXR requires VR support. Please check Virtual Reality Supported in Edit->Project Settings->Player->XR Settings</color>");
#pragma warning restore 618
#endif
            }
        }

        internal void Initialize()
        {
            if (UpdateInputManager != null)
                UpdateInputManager();

#if UNITY_EDITOR
            DrivenRectTransformTracker.StopRecordingUndo();

            if (!Application.isPlaying)
                ClearDeveloperConsoleIfNecessary();
#endif

            HandleInitialization();

            UnityBrandColorScheme.sessionGradient = UnityBrandColorScheme.GetRandomCuratedLightGradient();
            UnityBrandColorScheme.saturatedSessionGradient = UnityBrandColorScheme.GetRandomCuratedGradient();

            var delayedInitializationModules = new List<IDelayedInitializationModule>();
            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                var delayedInitializationModule = module as IDelayedInitializationModule;
                if (delayedInitializationModule != null)
                    delayedInitializationModules.Add(delayedInitializationModule);
            }

            delayedInitializationModules.Sort((a, b) => a.initializationOrder.CompareTo(b.initializationOrder));

            foreach (var module in delayedInitializationModules)
            {
                module.Initialize();
            }
        }

#if UNITY_EDITOR
        static void ClearDeveloperConsoleIfNecessary()
        {
            var asm = Assembly.GetAssembly(typeof(Editor));
            var consoleWindowType = asm.GetType("UnityEditor.ConsoleWindow");

            EditorWindow window = null;
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w.GetType() == consoleWindowType)
                {
                    window = w;
                    break;
                }
            }

            if (window)
            {
                var consoleFlagsType = consoleWindowType.GetNestedType("ConsoleFlags", BindingFlags.NonPublic);
                var names = Enum.GetNames(consoleFlagsType);
                var values = Enum.GetValues(consoleFlagsType);
                var clearOnPlayFlag = values.GetValue(Array.IndexOf(names, "ClearOnPlay"));

                var hasFlagMethod = consoleWindowType.GetMethod("HasFlag", BindingFlags.NonPublic | BindingFlags.Static);
                var result = (bool)hasFlagMethod.Invoke(window, new[] { clearOnPlayFlag });

                if (result)
                {
                    var logEntries = asm.GetType("UnityEditor.LogEntries");
                    var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                    clearMethod.Invoke(null, null);
                }
            }
        }
#endif

        internal void Shutdown()
        {
            var delayedInitializationModules = new List<IDelayedInitializationModule>();
            foreach (var module in ModuleLoaderCore.instance.modules)
            {
                var delayedInitializationModule = module as IDelayedInitializationModule;
                if (delayedInitializationModule != null)
                    delayedInitializationModules.Add(delayedInitializationModule);
            }

            delayedInitializationModules.Sort((a, b) => a.shutdownOrder.CompareTo(b.shutdownOrder));

            foreach (var module in delayedInitializationModules)
            {
                module.Shutdown();
            }

#if UNITY_EDITOR
            DrivenRectTransformTracker.StartRecordingUndo();
#endif
        }

        public void LoadModule() { }

        public void UnloadModule()
        {
#if UNITY_EDITOR
            var activeView = VRView.activeView;
            if (activeView)
                activeView.Close();
#endif
        }
    }
}
