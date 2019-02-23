#if !UNITY_CLOUD_BUILD
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
#if UNITY_2018_1_OR_NEWER
            var output = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Temp/" + target, target, BuildOptions.BuildScriptsOnly);
            if (output.steps.Length > 0)
            {
                foreach (var step in output.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.content.Contains("target is not supported"))
                            Assert.Inconclusive("Target platform {0} not installed", target);
                    }
                }
            }

            Assert.AreEqual(0, output.summary.totalErrors);
# else
            string output = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Temp/" + target, target, BuildOptions.BuildScriptsOnly);
            if (output.Contains("target is not supported"))
                Assert.Inconclusive("Target platform {0} not installed", target);

            Assert.IsFalse(output.Contains("error"));
#endif
        }

        [TearDown]
        public void TearDown()
        {
            if (m_VerifySavingAssets.HasValue)
                EditorPrefs.SetBool(k_VerifySavingAssets, m_VerifySavingAssets.Value);
        }
    }
}
#endif
