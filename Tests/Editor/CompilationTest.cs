using ConditionalCompilation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			defines.Remove("UNITY_EDITORVR");
			TestCompile(defines.ToArray());
		}

		static void TestCompile(string[] defines)
		{
			var references = new List<string>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				// Ignore project assemblies because they will cause conflicts
				if (assembly.FullName.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
					continue;

				// System.dll is included automatically and will cause conflicts if referenced explicitly
				if (assembly.FullName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
					continue;

				var codeBase = assembly.CodeBase;
				var uri = new UriBuilder(codeBase);
				var path = Uri.UnescapeDataString(uri.Path);
				references.Add(path);
			}

			var sources = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

			var outputFile = "Temp/CCUTest.dll";

			var output = EditorUtility.CompileCSharp(sources, references.ToArray(), defines, outputFile);

			foreach (var s in output)
			{
				Assert.IsFalse(s.Contains("error"), string.Join("\n", output));
			}
		}
	}
}
