using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class HierarchyModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrHierarchyModule = evr.GetModule<HierarchyModule>();

				var usesHierarchyData = obj as IUsesHierarchyData;
				if (usesHierarchyData != null)
				{
					evrHierarchyModule.AddConsumer(usesHierarchyData);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrHierarchyModule.AddConsumer(filterUI);
				}
			}

			public void DisconnectInterface(object obj)
			{
				var evrHierarchyModule = evr.GetModule<HierarchyModule>();

				var usesHierarchy = obj as IUsesHierarchyData;
				if (usesHierarchy != null)
				{
					evrHierarchyModule.RemoveConsumer(usesHierarchy);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrHierarchyModule.RemoveConsumer(filterUI);
				}
			}
		}
	}
}
