using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CCUTest
{
	const string kEditorPrefsKey = "EVR_TEST_CCU";
	const string kOldDefinesKey = "EVR_TEST_CCU_OLD_DEFINES";
	static bool compiled;
	static FieldInfo s_RunnerWindowInstanceField;
	static Type s_RunnerWindowType;

	static string s_ErrorLog;

	[Test]
	public void TestCCU()
	{
		if (compiled)
		{
			Assert.IsFalse(CheckErrors(), s_ErrorLog);
			if (EditorPrefs.HasKey(kOldDefinesKey))
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, EditorPrefs.GetString(kOldDefinesKey));
				EditorPrefs.DeleteKey(kOldDefinesKey);
			}
		}
		else
		{
			Debug.ClearDeveloperConsole();
			Application.logMessageReceived += Log;
			EditorPrefs.SetString(kOldDefinesKey, PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone));

			PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "");

			EditorPrefs.SetBool(kEditorPrefsKey, true);

			Assert.Inconclusive("Waiting for compile");
		}
	}

	static CCUTest()
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			s_RunnerWindowType = assembly.GetType("UnityEditor.EditorTestsRunner.EditorTestsRunnerWindow", false, true);
			if (s_RunnerWindowType != null)
				break;
		}

		s_RunnerWindowInstanceField = s_RunnerWindowType.GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
		
		EditorApplication.update -= Update;
		EditorApplication.update += Update;
	}

	static void Update()
	{
		if (!EditorApplication.isCompiling)
		{
			var runnerWindowInstance = s_RunnerWindowInstanceField.GetValue(null);

			if (runnerWindowInstance != null)
			{
				var test = EditorPrefs.GetBool(kEditorPrefsKey);
				if (test)
				{
					compiled = true;
					EditorPrefs.DeleteKey(kEditorPrefsKey);
					Application.logMessageReceived -= Log;
					s_RunnerWindowType.InvokeMember("RunTests", BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, runnerWindowInstance, null);
				}
			}

			if (CheckErrors() && EditorPrefs.HasKey(kOldDefinesKey))
			{
				Application.logMessageReceived -= Log;
				PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, EditorPrefs.GetString(kOldDefinesKey));
				EditorPrefs.DeleteKey(kOldDefinesKey);
			}
		}
	}

	static void Log(string logString, string stackTrace, LogType type)
	{
		s_ErrorLog += logString + '\n' + stackTrace + '\n';
	}

	static bool CheckErrors()
	{
		var assembly = Assembly.GetAssembly(typeof(SceneView));
		var logEntries = assembly.GetType("UnityEditorInternal.LogEntries");
		logEntries.GetMethod("Clear").Invoke (new object (), null);

		var count = (int)logEntries.GetMethod("GetCount").Invoke(new object(), null);

		return count > 0;
	}
}
