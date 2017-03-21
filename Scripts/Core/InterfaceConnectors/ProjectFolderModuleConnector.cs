using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class ProjectFolderModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrProjectFolderModule = evr.m_ProjectFolderModule;

				var usesProjectFolderData = obj as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
				{
					evrProjectFolderModule.AddConsumer(usesProjectFolderData);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrProjectFolderModule.AddConsumer(filterUI);
				}
			}

			public void DisconnectInterface(object obj)
			{
				var evrProjectFolderModule = evr.m_ProjectFolderModule;

				var usesProjectFolderData = obj as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
				{
					evrProjectFolderModule.RemoveConsumer(usesProjectFolderData);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrProjectFolderModule.RemoveConsumer(filterUI);
				}
			}
		}
	}

}
