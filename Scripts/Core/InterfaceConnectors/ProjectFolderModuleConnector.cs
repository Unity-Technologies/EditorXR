#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class ProjectFolderModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object target, object userData = null)
            {
                var usesProjectFolderData = target as IUsesProjectFolderData;
                if (usesProjectFolderData != null)
                {
                    var evrProjectFolderModule = evr.GetModule<ProjectFolderModule>();
                    evrProjectFolderModule.AddConsumer(usesProjectFolderData);

                    var filterUI = target as IFilterUI;
                    if (filterUI != null)
                        evrProjectFolderModule.AddConsumer(filterUI);
                }
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var usesProjectFolderData = target as IUsesProjectFolderData;
                if (usesProjectFolderData != null)
                {
                    var evrProjectFolderModule = evr.GetModule<ProjectFolderModule>();
                    evrProjectFolderModule.RemoveConsumer(usesProjectFolderData);

                    var filterUI = target as IFilterUI;
                    if (filterUI != null)
                        evrProjectFolderModule.RemoveConsumer(filterUI);
                }
            }
        }
    }
}
#endif