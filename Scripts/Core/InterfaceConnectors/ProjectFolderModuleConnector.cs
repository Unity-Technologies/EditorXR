#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class ProjectFolderModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object @object, object userData = null)
			{
				var usesProjectFolderData = @object as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
				{
					var evrProjectFolderModule = evr.GetModule<ProjectFolderModule>();
					evrProjectFolderModule.AddConsumer(usesProjectFolderData);

					var filterUI = @object as IFilterUI;
					if (filterUI != null)
						evrProjectFolderModule.AddConsumer(filterUI);
				}
			}

			public void DisconnectInterface(object @object, object userData = null)
			{
				var usesProjectFolderData = @object as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
				{
					var evrProjectFolderModule = evr.GetModule<ProjectFolderModule>();
					evrProjectFolderModule.RemoveConsumer(usesProjectFolderData);

					var filterUI = @object as IFilterUI;
					if (filterUI != null)
						evrProjectFolderModule.RemoveConsumer(filterUI);
				}
			}
		}
	}
}
#endif
