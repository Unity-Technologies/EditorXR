#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class ProjectFolderModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var usesProjectFolderData = obj as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
				{
					var evrProjectFolderModule = evr.GetModule<ProjectFolderModule>();
					evrProjectFolderModule.AddConsumer(usesProjectFolderData);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrProjectFolderModule.AddConsumer(filterUI);
				}
			}

			public void DisconnectInterface(object obj)
			{
				var usesProjectFolderData = obj as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
				{
					var evrProjectFolderModule = evr.GetModule<ProjectFolderModule>();
					evrProjectFolderModule.RemoveConsumer(usesProjectFolderData);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrProjectFolderModule.RemoveConsumer(filterUI);
				}
			}
		}
	}
}
#endif
