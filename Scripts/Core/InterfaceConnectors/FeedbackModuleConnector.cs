#if ENABLE_EDITORXR
namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class FeedbackModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object target, object userData = null)
            {
                var serializePreferences = target as IFeedbackReceiver;
                if (serializePreferences != null)
                    evr.GetModule<FeedbackModule>().AddReceiver(serializePreferences);
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var serializePreferences = target as IFeedbackReceiver;
                if (serializePreferences != null)
                    evr.GetModule<FeedbackModule>().RemoveReceiver(serializePreferences);
            }
        }
    }
}
#endif
