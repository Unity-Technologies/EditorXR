#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class HierarchyModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object target, object userData = null)
            {
                var usesHierarchyData = target as IUsesHierarchyData;
                if (usesHierarchyData != null)
                {
                    var evrHierarchyModule = evr.GetModule<HierarchyModule>();
                    evrHierarchyModule.AddConsumer(usesHierarchyData);

                    var filterUI = target as IFilterUI;
                    if (filterUI != null)
                        evrHierarchyModule.AddConsumer(filterUI);
                }
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var usesHierarchy = target as IUsesHierarchyData;
                if (usesHierarchy != null)
                {
                    var evrHierarchyModule = evr.GetModule<HierarchyModule>();
                    evrHierarchyModule.RemoveConsumer(usesHierarchy);

                    var filterUI = target as IFilterUI;
                    if (filterUI != null)
                        evrHierarchyModule.RemoveConsumer(filterUI);
                }
            }
        }
    }
}
#endif