using NUnit.Framework;
using Unity.Labs.EditorXR.Core;
using UnityEditor;

namespace Unity.Labs.EditorXR.Tests.Core
{
    [SetUpFixture]
    public class EditorVRTestInitializer
    {
        EditingContextManagerSettings projectSettingsBackup;
        EditingContextManagerSettings userSettingsBackup;

        [OneTimeSetUp]
        public void SetupBeforeAllTests()
        {
            projectSettingsBackup = EditingContextManager.LoadProjectSettings();
            userSettingsBackup = EditingContextManager.LoadUserSettings();

            EditingContextManager.ShowEditorVR();
        }

        [OneTimeTearDown]
        public void CleanupAfterAllTests()
        {
            EditingContextManager.SaveProjectSettings(projectSettingsBackup);
            EditingContextManager.SaveUserSettings(userSettingsBackup);

            EditorApplication.delayCall += () => { EditorWindow.GetWindow<VRView>("EditorVR", false).Close(); };
        }
    }
}
