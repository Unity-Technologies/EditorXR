using NUnit.Framework;

namespace UnityEditor.Experimental.EditorVR.Tests
{
    [InitializeOnLoad]
    public class BuildPlayerTest
    {
        const string k_VerifySavingAssets = "VerifySavingAssets";

        bool? m_VerifySavingAssets;

        [SetUp]
        public void SetUp()
        {
            if (EditorPrefs.HasKey(k_VerifySavingAssets))
            {
                m_VerifySavingAssets = EditorPrefs.GetBool(k_VerifySavingAssets);
                EditorPrefs.SetBool(k_VerifySavingAssets, false);
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

        static void TestBuildPlayer(BuildTarget target)
        {
            var output = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Temp/" + target, target, BuildOptions.BuildScriptsOnly);

            if (output.Contains("target is not supported"))
                Assert.Inconclusive("Target platform {0} not installed", target);

            Assert.IsFalse(output.Contains("error"));
        }

        [TearDown]
        public void TearDown()
        {
            if (m_VerifySavingAssets.HasValue)
                EditorPrefs.SetBool(k_VerifySavingAssets, m_VerifySavingAssets.Value);
        }
    }
}
