using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
			Assert.IsTrue(ConditionalCompilationUtility.enabled);
		}

		[Test]
		public void NoDependencyTest()
		{
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

			// This method has conflicts
			//var references = new List<string>();
			//Debug.Log(Assembly.GetCallingAssembly());
			//var referencedAssemblies = Assembly.GetCallingAssembly().MyGetReferencedAssembliesRecursive();

			//foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			//foreach (var assembly in referencedAssemblies.Values)
			//{
			//	Ignore project assemblies because they will cause conflicts
			//	if (assembly == typeof(ConditionalCompilationUtility).Assembly || assembly == typeof(EditorVR).Assembly)
			//		continue;

			//	var codeBase = assembly.CodeBase;
			//	var uri = new UriBuilder(codeBase);
			//	references.Add(Uri.UnescapeDataString(uri.Path));
			//}

			var sources = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

			var outputFile = Path.Combine("Temp", "CCUTest.dll");

			var defines = EditorUserBuildSettings.activeScriptCompilationDefines.ToList();
			defines = defines.Except(ConditionalCompilationUtility.defines).ToList();

			var output = EditorUtility.CompileCSharp(sources, references.ToArray(), defines.ToArray(), outputFile);

			foreach (var s in output)
			{
				Assert.IsFalse(s.Contains("error"), string.Join("\n", output));
			}
		}

	}

	public static class AssemblyExtensions
	{
		private static Dictionary<string, Assembly> _dependentAssemblyList;

		//private static List<MissingAssembly> _missingAssemblyList;
		public static List<string> MyGetReferencedAssembliesFlat(this Type type)
		{
			var results = type.Assembly.GetReferencedAssemblies();
			return results.Select(o => o.FullName).OrderBy(o => o).ToList();
		}

		public static Dictionary<string, Assembly> MyGetReferencedAssembliesRecursive(this Assembly assembly)
		{
			Debug.Log(assembly);
			_dependentAssemblyList = new Dictionary<string, Assembly>();

			//_missingAssemblyList = new List<MissingAssembly>();

			InternalGetDependentAssembliesRecursive(assembly);

			// Only include assemblies that we wrote ourselves (ignore ones from GAC).
			var keysToRemove = _dependentAssemblyList.Values.Where(
				o => o.GlobalAssemblyCache == true).ToList();

			foreach (var k in keysToRemove)
			{
				_dependentAssemblyList.Remove(k.FullName.MyToName());
			}

			return _dependentAssemblyList;
		}

		private static void InternalGetDependentAssembliesRecursive(Assembly assembly)
		{
			// Load assemblies with newest versions first. Omitting the ordering results in false positives on
			// _missingAssemblyList.
			var referencedAssemblies = assembly.GetReferencedAssemblies()
				.OrderByDescending(o => o.Version);

			foreach (var r in referencedAssemblies)
			{
				if (String.IsNullOrEmpty(assembly.FullName))
				{
					continue;
				}

				if (_dependentAssemblyList.ContainsKey(r.FullName.MyToName()) == false)
				{
					try
					{
						var a = Assembly.ReflectionOnlyLoad(r.FullName);
						_dependentAssemblyList[a.FullName.MyToName()] = a;
						InternalGetDependentAssembliesRecursive(a);
					}
					catch (Exception ex)
					{
						//_missingAssemblyList.Add(new MissingAssembly(r.FullName.Split(',')[0], assembly.FullName.MyToName()));
					}
				}
			}
		}

		private static string MyToName(this string fullName)
		{
			return fullName.Split(',')[0];
		}
	}
}
