using NUnit.Framework;

namespace UnityEditor.Experimental.EditorVR.Tests
{
	[InitializeOnLoad]
	public class BuildPlayerTest
	{
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
		public void StandaloneWindows()
		{
			TestBuildPlayer(BuildTarget.StandaloneWindows);
		}

		[Test]
		public void StandaloneWindows64()
		{
			TestBuildPlayer(BuildTarget.StandaloneWindows64);
		}

		[Test]
		public void Android()
		{
			TestBuildPlayer(BuildTarget.Android);
		}

		[Test]
		public void WebGL()
		{
			TestBuildPlayer(BuildTarget.WebGL);
		}

		static void TestBuildPlayer(BuildTarget target)
		{
			var output = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Temp/" + target, target, BuildOptions.None);

			if (output.Contains("target is not supported"))
				Assert.Inconclusive("Target platform {0} not installed", target);

			Assert.IsFalse(output.Contains("error"));
		}

		[TearDown]
		public void TearDown()
		{
			if (m_VerifySavingAssets.HasValue)
				EditorPrefs.SetBool(kVerifySavingAssets, m_VerifySavingAssets.Value);
		}
	}
}
