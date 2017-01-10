using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CCUTest
{
	[Test]
	public void TestCCU()
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
		var defines = new []
		{
			"UNITY_EDITOR",
			"UNITY_5_3_OR_NEWER"
		};
		var output = EditorUtility.CompileCSharp(sources.ToArray(), references.ToArray(), defines, "test.dll");
		foreach (var s in output)
		{
			Assert.IsFalse(s.Contains("error"));
		}
	}

	static void GetAllFiles(string path, List<string> files, string searchPattern)
	{
		try
		{
			foreach (var f in Directory.GetFiles(path, searchPattern))
			{
				files.Add(f);
			}
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
