// Edit mode support requires legacy VR, which was removed in 2020.1
#if UNITY_EDITOR && !UNITY_2020_1_OR_NEWER
#define UNITY_EDITORXR_EDIT_MODE_SUPPORT
#endif

using NUnit.Framework;
using Unity.EditorXR.Core;
using UnityEditor;

namespace Unity.EditorXR.Tests.Core
{
    [SetUpFixture]
    public class EditorXRTestInitializer
    {
        EditingContextManagerSettings projectSettingsBackup;
        EditingContextManagerSettings userSettingsBackup;

        [OneTimeSetUp]
        public void SetupBeforeAllTests()
        {
            projectSettingsBackup = EditingContextManager.LoadProjectSettings();
            userSettingsBackup = EditingContextManager.LoadUserSettings();

#if UNITY_EDITORXR_EDIT_MODE_SUPPORT
            EditingContextManager.ShowEditorXR();
#endif
        }

        [OneTimeTearDown]
        public void CleanupAfterAllTests()
        {
            EditingContextManager.SaveProjectSettings(projectSettingsBackup);
            EditingContextManager.SaveUserSettings(userSettingsBackup);

            EditorApplication.delayCall += () => { EditorWindow.GetWindow<VRView>("EditorXR", false).Close(); };
        }
    }
}
