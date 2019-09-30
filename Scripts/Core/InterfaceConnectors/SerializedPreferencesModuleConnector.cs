#if UNITY_2018_4_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SerializedPreferencesModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object target, object userData = null)
            {
                var serializePreferences = target as ISerializePreferences;
                if (serializePreferences != null)
                    evr.GetModule<SerializedPreferencesModule>().AddSerializer(serializePreferences);
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var serializePreferences = target as ISerializePreferences;
                if (serializePreferences != null)
                    evr.GetModule<SerializedPreferencesModule>().RemoveSerializer(serializePreferences);
            }
        }
    }
}
#endif
