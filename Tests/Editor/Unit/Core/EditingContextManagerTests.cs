// Edit mode support requires legacy VR, which was removed in 2020.1
#if UNITY_EDITOR && !UNITY_2020_1_OR_NEWER
#define UNITY_EDITORXR_EDIT_MODE_SUPPORT
#endif

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.EditorXR.Core;
using Unity.EditorXR.Tools;
using Unity.XRTools.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.Tests.Core
{
    [TestFixture]
    class EditingContextManagerTests
    {
        GameObject go;
        EditorXRContext context, context2;
        EditingContextManager manager;
        EditingContextManagerSettings settings, newSettings;
        SetEditingContextImplementor contextSetter;

        [OneTimeSetUp]
        public void Setup()
        {
            manager = EditingContextManager.instance;
            go = new GameObject("context test object");
            var transformTool = go.AddComponent<TransformTool>();
            var createPrimitiveTool = go.AddComponent<CreatePrimitiveTool>();

            context = ScriptableObject.CreateInstance<EditorXRContext>();
            context.name = "Some Other Context";
            context.m_DefaultToolStack = new List<MonoScript>();
            context.m_DefaultToolStack.Add(MonoScript.FromMonoBehaviour(transformTool));
            context.m_DefaultToolStack.Add(MonoScript.FromMonoBehaviour(createPrimitiveTool));
            UnityObjectUtils.Destroy(go);

            context2 = ScriptableObject.CreateInstance<EditorXRContext>();
            context2.name = "Yet Another Context";
            context2.m_DefaultToolStack = context.m_DefaultToolStack;

            settings = ScriptableObject.CreateInstance<EditingContextManagerSettings>();
            settings.defaultContextName = "Custom Default Context";
            newSettings = ScriptableObject.CreateInstance<EditingContextManagerSettings>();
            newSettings.defaultContextName = "New Custom Default Context";

            // Save once so that we can detect a change--without this, SaveProjectSettings_UpdatesProjectSettingsFile will fail on CloudBuild
            EditingContextManager.SaveProjectSettings(settings);
        }

#if UNITY_EDITORXR_EDIT_MODE_SUPPORT
        [Test]
        public void Initializes_WithDefaultContext()
        {
            Assert.AreEqual(EditingContextManager.defaultContext, manager.currentContext);
        }

        [Test]
        public void Initializes_ISetEditingContextMethods()
        {
            Assert.IsNotNull(SetEditingContextMethods.getAvailableEditingContexts);
            Assert.IsNotNull(SetEditingContextMethods.getPreviousEditingContexts);
            Assert.IsNotNull(SetEditingContextMethods.setEditingContext);
            Assert.IsNotNull(SetEditingContextMethods.restorePreviousEditingContext);
        }

        [Test]
        public void SetEditingContext_DoesNothing_IfNull()
        {
            var beginningContext = manager.currentContext;
            manager.SetEditingContext(null);
            Assert.AreEqual(beginningContext, manager.currentContext);
        }

        [Test]
        public void SetEditingContext_SetsCurrentContext_IfNotNull()
        {
            manager.SetEditingContext(context);
            Assert.AreEqual(context, manager.currentContext);
        }

        [Test]
        public void RestorePreviousContext_SetsPreviousContextToCurrent()
        {
            var beginningContext = manager.currentContext;
            manager.SetEditingContext(context2);
            Assert.AreNotEqual(beginningContext, manager.currentContext);

            manager.RestorePreviousContext();
            Assert.AreEqual(beginningContext, manager.currentContext);
        }
#endif

        [Test]
        public void LoadProjectSettings_IfAssetFound()
        {
            EditingContextManager.SaveProjectSettings(settings);
            var loaded = EditingContextManager.LoadProjectSettings();
            Assert.AreEqual(settings.defaultContextName, loaded.defaultContextName);
        }

        [Test]
        public void LoadProjectSettings_IfAssetNotFound()
        {
            if (File.Exists(EditingContextManager.settingsPath))
                File.Delete(EditingContextManager.settingsPath);

            var loaded = EditingContextManager.LoadProjectSettings();
            Assert.IsInstanceOf<EditingContextManagerSettings>(loaded);
            Assert.IsNull(loaded.defaultContextName);
        }

        [Test]
        public void LoadUserSettings_NewerThanProjectSettings()
        {
            EditingContextManager.SaveUserSettings(newSettings);
            var loaded = EditingContextManager.LoadUserSettings();
            Assert.AreEqual(newSettings.defaultContextName, loaded.defaultContextName);
        }

        [Test]
        public void LoadUserSettings_OlderThanProjectSettings()
        {
            EditingContextManager.SaveUserSettings(newSettings);
            EditingContextManager.SaveProjectSettings(settings);

            var loaded = EditingContextManager.LoadUserSettings();
            Assert.AreEqual(settings.defaultContextName, loaded.defaultContextName);
        }

        [Test]
        public void LoadUserSettings_ProjectSettingsFallback()
        {
            if (File.Exists(EditingContextManager.userSettingsPath))
                File.Delete(EditingContextManager.userSettingsPath);

            var projectSettings = EditingContextManager.LoadProjectSettings();
            var userSettings = EditingContextManager.LoadUserSettings();

            Assert.AreEqual(projectSettings.defaultContextName, userSettings.defaultContextName);
        }

        [Test]
        public void SaveProjectSettings_UpdatesProjectSettingsFile()
        {
            var path = EditingContextManager.settingsPath;
            var lastModTime = File.GetLastWriteTime(path);
            Thread.Sleep(1000); // Wait one second to make sure modified time is later
            EditingContextManager.SaveProjectSettings(settings);

            Assert.AreEqual(JsonUtility.ToJson(settings, true), File.ReadAllText(path));
            Assert.Greater(File.GetLastWriteTime(path), lastModTime);
        }

        [Test]
        public void SaveUserSettings_UpdatesUserSettingsFile()
        {
            var path = EditingContextManager.userSettingsPath;
            var lastModTime = File.GetLastWriteTime(path);
            EditingContextManager.SaveUserSettings(newSettings);

            Assert.AreEqual(JsonUtility.ToJson(newSettings, true), File.ReadAllText(path));
            Assert.Greater(File.GetLastWriteTime(path), lastModTime);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
#if UNITY_EDITORXR_EDIT_MODE_SUPPORT
            manager.SetEditingContext(EditingContextManager.defaultContext);
#endif
            UnityObjectUtils.Destroy(context);
            UnityObjectUtils.Destroy(context2);

#if UNITY_EDITORXR_EDIT_MODE_SUPPORT
            VRView.activeView.Close();
#endif
        }
    }

    class SetEditingContextImplementor : ISetEditingContext
    {
    }
}
