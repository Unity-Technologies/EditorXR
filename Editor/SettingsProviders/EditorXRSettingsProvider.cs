using UnityEditor;

namespace Unity.Labs.EditorXR.UI
{
    class EditorXRSettingsProvider : SettingsProvider
    {
        protected const string k_Path = "Project/EditorXR";

        protected EditorXRSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }


        [SettingsProvider]
        public static SettingsProvider CreateEditorXRSettingsProvider()
        {
            var provider = new EditorXRSettingsProvider(k_Path);
            return provider;
        }
    }
}
