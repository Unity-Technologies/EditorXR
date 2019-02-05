using ConditionalCompilation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tests
{
    [InitializeOnLoad]
    public class CompilationTest
    {
        [Test]
        public void CCUEnabled()
        {
            Assert.IsTrue(ConditionalCompilationUtility.enabled);
        }

        [Test]
        public void NoCCUDefines()
        {
            var defines = EditorUserBuildSettings.activeScriptCompilationDefines.ToList();
            defines = defines.Except(ConditionalCompilationUtility.defines).ToList();
            TestCompile(defines.ToArray());
        }

        [Test]
        public void NoEditorVR()
        {
            var defines = EditorUserBuildSettings.activeScriptCompilationDefines.ToList();
            defines.Remove("UNITY_2017_2_OR_NEWER");
            TestCompile(defines.ToArray());
        }

        static void TestCompile(string[] defines)
        {
            var outputFile = "Temp/CCUTest.dll";

            var references = new List<string>();
            ObjectUtils.ForEachAssembly(assembly =>
            {
                // Ignore project assemblies because they will cause conflicts
                if (assembly.FullName.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                    return;

                // System.dll is included automatically and will cause conflicts if referenced explicitly
                if (assembly.FullName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                    return;

                // This assembly causes a ReflectionTypeLoadException on compile
                if (assembly.FullName.StartsWith("ICSharpCode.NRefactory", StringComparison.OrdinalIgnoreCase))
                    return;

                if (assembly.FullName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase))
                    return;

                var codeBase = assembly.CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);

                references.Add(path);
            });

            var sources = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            //TODO: Rename EditorVR class to EditorXR after merging playmode
            var editorVRFolder = sources.Any(s => s.Contains("EditorVR.cs") && s.Replace("EditorVR.cs", string.Empty).Contains("EditorXR"));
            Assert.IsTrue(editorVRFolder, "EditorXR scripts must be under a folder named 'EditorXR'");

            var output = EditorUtility.CompileCSharp(sources, references.ToArray(), defines, outputFile);
            foreach (var o in output)
            {
                var line = o.ToLower();
                if (line.Contains("editorxr"))
                    Assert.IsFalse(line.Contains("exception") || line.Contains("error") || line.Contains("warning"), string.Join("\n", output));
            }
        }
    }
}
