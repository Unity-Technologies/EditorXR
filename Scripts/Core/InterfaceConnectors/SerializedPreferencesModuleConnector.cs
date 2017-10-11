#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SerializedPreferencesModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object @object, object userData = null)
            {
                var serializePreferences = @object as ISerializePreferences;
                if (serializePreferences != null)
                    evr.GetModule<SerializedPreferencesModule>().AddSerializer(serializePreferences);
            }

            public void DisconnectInterface(object @object, object userData = null)
            {
                var serializePreferences = @object as ISerializePreferences;
                if (serializePreferences != null)
                    evr.GetModule<SerializedPreferencesModule>().RemoveSerializer(serializePreferences);
            }
        }
    }
}
#endif
