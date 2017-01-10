using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Tests
{
	[InitializeOnLoad]
	public class BuildPlayerTest
	{
		const string kTestPath = "_Test";
		const string kVerifySavingAssets = "VerifySavingAssets";

		bool? m_VerifySavingAssets;

		[SetUp]
		public void SetUp()
		{
			if (EditorPrefs.HasKey(kVerifySavingAssets))
			{
				m_VerifySavingAssets = EditorPrefs.GetBool(kVerifySavingAssets);
				EditorPrefs.SetBool(kVerifySavingAssets, false);
			}
		}

		[Test]
		public void TestStandaloneWindows()
		{
			TestBuildPlayer(BuildTarget.StandaloneWindows);
		}

		[Test]
		public void TestStandaloneWindows64()
		{
			TestBuildPlayer(BuildTarget.StandaloneWindows64);
		}

		[Test]
		public void TestAndroid()
		{
			TestBuildPlayer(BuildTarget.Android);
		}

		[Test]
		public void TestWebGL()
		{
			TestBuildPlayer(BuildTarget.WebGL);
		}

		[Test]
		public void TestStandaloneLinux()
		{
			TestBuildPlayer(BuildTarget.StandaloneLinux);
		}

		[Test]
		public void TestStandaloneLinux64()
		{
			TestBuildPlayer(BuildTarget.StandaloneLinux64);
		}

		static void TestBuildPlayer(BuildTarget target)
		{
			var output = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, Path.Combine(kTestPath, target.ToString()), target, BuildOptions.None);

			if (output.Contains("target is not supported"))
				Assert.Inconclusive("Target platform {0} not installed", target);

			Assert.IsFalse(output.Contains("error"));
		}

		[TearDown]
		public void TearDown()
		{
			if (m_VerifySavingAssets.HasValue)
				EditorPrefs.SetBool(kVerifySavingAssets, m_VerifySavingAssets.Value);

			if (!Directory.Exists(kTestPath))
				return;

			try
			{
				Directory.Delete(kTestPath, true);
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("BuildPlayerTest: Could not delete temp directory {0}: {1}", kTestPath, e.Message));
			}
		}
	}
}
