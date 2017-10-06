#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class HierarchyModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object @object, object userData = null)
			{
				var usesHierarchyData = @object as IUsesHierarchyData;
				if (usesHierarchyData != null)
				{
					var evrHierarchyModule = evr.GetModule<HierarchyModule>();
					evrHierarchyModule.AddConsumer(usesHierarchyData);

					var filterUI = @object as IFilterUI;
					if (filterUI != null)
						evrHierarchyModule.AddConsumer(filterUI);
				}
			}

			public void DisconnectInterface(object @object, object userData = null)
			{
				var usesHierarchy = @object as IUsesHierarchyData;
				if (usesHierarchy != null)
				{
					var evrHierarchyModule = evr.GetModule<HierarchyModule>();
					evrHierarchyModule.RemoveConsumer(usesHierarchy);

					var filterUI = @object as IFilterUI;
					if (filterUI != null)
						evrHierarchyModule.RemoveConsumer(filterUI);
				}
			}
		}
	}
}
#endif
