#if UNITY_EDITOR && UNITY_EDITORVR
namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class FeedbackModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object @object, object userData = null)
			{
				var serializePreferences = @object as IFeedbackReceiver;
				if (serializePreferences != null)
					evr.GetModule<FeedbackModule>().AddReceiver(serializePreferences);
			}

			public void DisconnectInterface(object @object, object userData = null)
			{
				var serializePreferences = @object as IFeedbackReceiver;
				if (serializePreferences != null)
					evr.GetModule<FeedbackModule>().RemoveReceiver(serializePreferences);
			}
		}
	}
}
#endif
