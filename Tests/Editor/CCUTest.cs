using System;
using System.Collections.Generic;
using System.IO;
using ConditionalCompilation;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tests
{
	[InitializeOnLoad]
	public class CCUTest
	{
		[Test]
		public void TestCCU()
		{
			Assert.IsTrue(ConditionalCompilationUtility.kEnabled);
		}

		[Test]
		public void NoDependencyTest()
		{
			// TODO: Find a better way to collect dependencies
			var dependencyPath = Path.Combine(EditorApplication.applicationContentsPath, "Managed");
			var extensionsPath = Path.Combine(EditorApplication.applicationContentsPath, "UnityExtensions");

			var references = new List<string>
			{
				Path.Combine(dependencyPath, "UnityEngine.dll"),
				Path.Combine(dependencyPath, "UnityEditor.dll"),

				Path.Combine(Path.Combine(Path.Combine(extensionsPath, "Unity"), "GUISystem"), "UnityEngine.UI.dll"),
				Path.Combine(Path.Combine(Path.Combine(Path.Combine(extensionsPath, "Unity"), "GUISystem"), "Editor"), "UnityEditor.UI.dll"),
				Path.Combine(Path.Combine(Path.Combine(Path.Combine(extensionsPath, "Unity"), "EditorTestsRunner"), "Editor"), "nunit.framework.dll"),
				Path.Combine(Path.Combine(Path.Combine(Path.Combine(extensionsPath, "Unity"), "EditorTestsRunner"), "Editor"), "UnityEditor.EditorTestsRunner.dll")
			};

			//GetAllFiles(EditorApplication.applicationContentsPath, references, "*.dll"); // Need to weed out unmanaged dlls
			//GetAllFiles(dependencyPath, references, "*.dll"); // Error on loading ICsharpCode.NRefactory

			var sources = new List<string>();
			GetAllFiles(Application.dataPath, sources, "*.cs");

			// TODO: Find a better way to collect #defines
			var defines = new[]
			{
				"UNITY_EDITOR",
				"UNITY_5_3_OR_NEWER"
			};

			const string outputFile = "CCUTest.dll";

			var output = EditorUtility.CompileCSharp(sources.ToArray(), references.ToArray(), defines, outputFile);
			try
			{
				File.Delete(outputFile);
				File.Delete(outputFile + ".mdb");
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("CCUTest: Could not delete temp files: {0}", e.Message));
			}

			foreach (var s in output)
			{
				Assert.IsFalse(s.Contains("error"), string.Join("\n", output));
			}
		}

		static void GetAllFiles(string path, List<string> files, string searchPattern)
		{
			try
			{
				files.AddRange(Directory.GetFiles(path, searchPattern));
				foreach (var d in Directory.GetDirectories(path))
				{
					GetAllFiles(d, files, searchPattern);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}
