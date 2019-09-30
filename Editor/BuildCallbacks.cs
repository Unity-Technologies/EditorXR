#if ENABLE_EDITORXR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    class BuildCallbacks : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
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

            public string Name { get { return name; } set { name = value; } }
            public string[] IncludePlatforms { get { return includePlatforms; } set { includePlatforms = value; } }
            public string[] ExcludePlatforms { get { return excludePlatforms; } set { excludePlatforms = value; } }
        }

        static readonly string[] k_AssemblyNames = { "EXR", "EXR-Dependencies", "input-prototype", "VRLR" };
        static readonly string[] k_IncludePlatformsOverride = { "Editor" };
        static readonly string[] k_ExcludePlatformsOverride = { };
        static readonly Dictionary<string, string[]> k_IncludePlatforms = new Dictionary<string, string[]>();
        static readonly Dictionary<string, string[]> k_ExcludePlatforms = new Dictionary<string, string[]>();

        static bool s_IsBuilding;

        public int callbackOrder { get { return 0; } }

        static void ForEachAssembly(Action<AssemblyDefinition> callback)
        {
            foreach (var assembly in k_AssemblyNames)
            {
                var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarningFormat("Error in EditorXR Pre-Build action: Cannot find asmdef for assembly: {0}", assembly);
                    continue;
                }

                var asmDefAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                if (asmDefAsset == null)
                {
                    Debug.LogWarningFormat("Error in EditorXR Pre-Build action: Cannot load asmdef at: {0}", path);
                    continue;
                }

                var asmDef = JsonUtility.FromJson<AssemblyDefinition>(asmDefAsset.text);
                callback(asmDef);
                File.WriteAllText(path, JsonUtility.ToJson(asmDef, true));
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (Core.EditorVR.includeInBuilds)
                return;

            s_IsBuilding = true;
            EditorApplication.update += CheckBuildComplete;

            ForEachAssembly(asmDef =>
            {
                var name = asmDef.Name;
                k_IncludePlatforms[name] = asmDef.IncludePlatforms;
                k_ExcludePlatforms[name] = asmDef.ExcludePlatforms;

                asmDef.IncludePlatforms = k_IncludePlatformsOverride;
                asmDef.ExcludePlatforms = k_ExcludePlatformsOverride;
            });

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            OnPostprocessBuild();
        }

        public void OnPostprocessBuild()
        {
            s_IsBuilding = false;
            EditorApplication.update -= CheckBuildComplete;

            if (Core.EditorVR.includeInBuilds)
                return;

            ForEachAssembly(asmDef =>
            {
                var name = asmDef.Name;
                asmDef.IncludePlatforms = k_IncludePlatforms[name];
                asmDef.ExcludePlatforms = k_ExcludePlatforms[name];
            });
        }

        void CheckBuildComplete()
        {
            if (s_IsBuilding)
                OnPostprocessBuild();
        }
    }
}
#endif
