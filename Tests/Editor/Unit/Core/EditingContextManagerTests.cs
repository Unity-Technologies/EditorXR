#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Tools;

namespace UnityEditor.Experimental.EditorVR.Tests.Core
{
    [TestFixture]
    public class EditingContextManagerTests
    {
        GameObject go;
        EditorVRContext context, context2;
        EditingContextManager manager;
        EditingContextManagerSettings settings, newSettings;
        SetEditingContextImplementor contextSetter;

        [OneTimeSetUp]
        public void Setup()
        {
            manager = EditingContextManager.s_Instance;
            go = new GameObject("context test object");
            var transformTool = go.AddComponent<TransformTool>();
            var createPrimitiveTool = go.AddComponent<CreatePrimitiveTool>();

            context = ScriptableObject.CreateInstance<EditorVRContext>();
            context.name = "Some Other Context";
            context.m_DefaultToolStack = new List<MonoScript>();
            context.m_DefaultToolStack.Add(MonoScript.FromMonoBehaviour(transformTool));
            context.m_DefaultToolStack.Add(MonoScript.FromMonoBehaviour(createPrimitiveTool));

            context2 = ScriptableObject.CreateInstance<EditorVRContext>();
            context2.name = "Yet Another Context";
            context2.m_DefaultToolStack = context.m_DefaultToolStack;

            settings = ScriptableObject.CreateInstance<EditingContextManagerSettings>();
            settings.defaultContextName = "Custom Default Context";
            newSettings = ScriptableObject.CreateInstance<EditingContextManagerSettings>();
            newSettings.defaultContextName = "New Custom Default Context";

            // Save once so that we can detect a change--without this, SaveProjectSettings_UpdatesProjectSettingsFile will fail on CloudBuild
            EditingContextManager.SaveProjectSettings(settings);
        }

        [Test]
        public void Initializes_WithDefaultContext()
        {
            Assert.AreEqual(EditingContextManager.defaultContext, manager.currentContext);
        }

        [Test]
        public void Initializes_ISetEditingContextMethods()
        {
            Assert.IsNotNull(ISetEditingContextMethods.getAvailableEditingContexts);
            Assert.IsNotNull(ISetEditingContextMethods.getPreviousEditingContexts);
            Assert.IsNotNull(ISetEditingContextMethods.setEditingContext);
            Assert.IsNotNull(ISetEditingContextMethods.restorePreviousEditingContext);
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
            if (File.Exists(EditingContextManager.k_SettingsPath))
                File.Delete(EditingContextManager.k_SettingsPath);

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
            if (File.Exists(EditingContextManager.k_UserSettingsPath))
                File.Delete(EditingContextManager.k_UserSettingsPath);

            var projectSettings = EditingContextManager.LoadProjectSettings();
            var userSettings = EditingContextManager.LoadUserSettings();

            Assert.AreEqual(projectSettings.defaultContextName, userSettings.defaultContextName);
        }

        [Test]
        public void SaveProjectSettings_UpdatesProjectSettingsFile()
        {
            var path = EditingContextManager.k_SettingsPath;
            var lastModTime = File.GetLastWriteTime(path);
            EditingContextManager.SaveProjectSettings(settings);

            Assert.AreEqual(JsonUtility.ToJson(settings, true), File.ReadAllText(path));
            Assert.Greater(File.GetLastWriteTime(path), lastModTime);
        }

        [Test]
        public void SaveUserSettings_UpdatesUserSettingsFile()
        {
            var path = EditingContextManager.k_UserSettingsPath;
            var lastModTime = File.GetLastWriteTime(path);
            EditingContextManager.SaveUserSettings(newSettings);

            Assert.AreEqual(JsonUtility.ToJson(newSettings, true), File.ReadAllText(path));
            Assert.Greater(File.GetLastWriteTime(path), lastModTime);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            ObjectUtils.Destroy(go);
            manager.SetEditingContext(EditingContextManager.defaultContext);
            ObjectUtils.Destroy(context);
            ObjectUtils.Destroy(context2);
        }
    }

    class SetEditingContextImplementor : ISetEditingContext { }
}
#endif
