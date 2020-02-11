using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    [InitializeOnLoad]
    static class PolyToolkitHelper
    {
        // ReSharper disable InconsistentNaming
        [Serializable]
        class AssemblyDefinition
        {
            [SerializeField]
            string name;

            [SerializeField]
            string[] references;

            [SerializeField]
            string[] optionalUnityReferences;

            [SerializeField]
            string[] includePlatforms;

            [SerializeField]
            string[] excludePlatforms;

            [SerializeField]
            bool allowUnsafeCode;

            public string Name
            {
                get { return name; }
                set { name = value; }
            }

            public string[] References
            {
                get { return references; }
                set { references = value; }
            }

            public string[] IncludePlatforms
            {
                get { return includePlatforms; }
                set { includePlatforms = value; }
            }

            public bool AllowUnsafeCode
            {
                get { return allowUnsafeCode; }
                set { allowUnsafeCode = value; }
            }
        }

        // ReSharper restore InconsistentNaming

        const string k_PolyApiGuid = "bffc1b9d57b39eb47a9ca38dcb349814";
        const string k_PtUtilsGuid = "c92cab08cb256b44282cc813e8abd250";
        const string k_AssemblyDefinitionExtension = ".asmdef";
        const string k_RuntimeAssemblyDefinitionName = "PolyToolkit";
        const string k_EditorAssemblyDefinitionName = "PolyToolkit.Editor";
        const string k_RuntimeAssemblyDefinitionFileName = k_RuntimeAssemblyDefinitionName + k_AssemblyDefinitionExtension;
        const string k_EditorAssemblyDefinitionFileName = k_EditorAssemblyDefinitionName + k_AssemblyDefinitionExtension;
        const string k_IncludePolyToolkitDefine = "INCLUDE_POLY_TOOLKIT";
        static readonly string[] k_IncludePlatformsEditorOnly = { "Editor" };

        static PolyToolkitHelper()
        {
            // Use compilationFinished instead of just running in the static constructor to catch failed compiles
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        static void OnCompilationFinished(object context)
        {
            var polyApiPath = AssetDatabase.GUIDToAssetPath(k_PolyApiGuid);

            // Load the asset in case it was recently deleted, and its guid is still loaded in the database
            if (string.IsNullOrEmpty(polyApiPath) || AssetDatabase.LoadAssetAtPath<MonoScript>(polyApiPath) == null)
            {
                Cleanup();
                return;
            }

            var runtimeFolder = Directory.GetParent(polyApiPath).Parent;
            if (runtimeFolder == null)
            {
                Cleanup();
                return;
            }

            var ptUtilsPath = AssetDatabase.GUIDToAssetPath(k_PtUtilsGuid);
            if (string.IsNullOrEmpty(ptUtilsPath))
            {
                Cleanup();
                return;
            }

            var editorFolder = Directory.GetParent(ptUtilsPath);
            if (editorFolder == null)
            {
                Cleanup();
                return;
            }

            var runtimeAssemblyDefinitionPath = Path.Combine(runtimeFolder.ToString(), k_RuntimeAssemblyDefinitionFileName);
            if (!File.Exists(runtimeAssemblyDefinitionPath))
            {
                var runtimeAssemblyDefinition = new AssemblyDefinition
                {
                    Name = k_RuntimeAssemblyDefinitionName,
                    AllowUnsafeCode = true
                };

                File.WriteAllText(runtimeAssemblyDefinitionPath, JsonUtility.ToJson(runtimeAssemblyDefinition));
            }

            var editorAssemblyDefinitionPath = Path.Combine(editorFolder.ToString(), k_EditorAssemblyDefinitionFileName);
            if (!File.Exists(editorAssemblyDefinitionPath))
            {
                var editorAssemblyDefinition = new AssemblyDefinition
                {
                    Name = k_EditorAssemblyDefinitionName,
                    References = new[] { k_RuntimeAssemblyDefinitionName },
                    IncludePlatforms = k_IncludePlatformsEditorOnly
                };

                File.WriteAllText(editorAssemblyDefinitionPath, JsonUtility.ToJson(editorAssemblyDefinition));
            }

            BuildTargetGroup currentGroup;
            var defineString = GetDefineString(out currentGroup);
            if (string.IsNullOrEmpty(defineString) || defineString.Contains(k_IncludePolyToolkitDefine))
                return;

            var defines = defineString.Split(';').ToList();
            defines.Add(k_IncludePolyToolkitDefine);
            defineString = string.Join(";", defines);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(currentGroup, defineString);
        }

        /// <summary>
        /// Attempt to remove the INCLUDE_POLY_TOOLKIT define if it exists, but PolyToolkit cannot be found
        /// </summary>
        static void Cleanup()
        {
            BuildTargetGroup currentGroup;
            var defineString = GetDefineString(out currentGroup);
            if (string.IsNullOrEmpty(defineString) || !defineString.Contains(k_IncludePolyToolkitDefine))
                return;

            var defines = defineString.Split(';').ToList();
            defines.Remove(k_IncludePolyToolkitDefine);
            defineString = string.Join(";", defines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(currentGroup, defineString);
        }

        static string GetDefineString(out BuildTargetGroup currentGroup)
        {
            currentGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(currentGroup);
        }
    }
}
