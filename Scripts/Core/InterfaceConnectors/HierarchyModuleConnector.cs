#if UNITY_EDITOR && UNITY_EDITORVR
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
				var usesHierarchyData = obj as IUsesHierarchyData;
				if (usesHierarchyData != null)
				{
					var evrHierarchyModule = evr.GetModule<HierarchyModule>();
					evrHierarchyModule.AddConsumer(usesHierarchyData);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrHierarchyModule.AddConsumer(filterUI);
				}
			}

			public void DisconnectInterface(object obj)
			{
				var usesHierarchy = obj as IUsesHierarchyData;
				if (usesHierarchy != null)
				{
					var evrHierarchyModule = evr.GetModule<HierarchyModule>();
					evrHierarchyModule.RemoveConsumer(usesHierarchy);

					var filterUI = obj as IFilterUI;
					if (filterUI != null)
						evrHierarchyModule.RemoveConsumer(filterUI);
				}
			}
		}
	}
}
#endif
