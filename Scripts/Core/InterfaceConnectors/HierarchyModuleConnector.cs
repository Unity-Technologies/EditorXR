using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class HierarchyModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrHierarchyModule = evr.m_HierarchyModule;

				var usesHierarchyData = obj as IUsesHierarchyData;
				if (usesHierarchyData != null)
					evrHierarchyModule.AddConsumer(usesHierarchyData);
			}

			public void DisconnectInterface(object obj)
			{
				var evrHierarchyModule = evr.m_HierarchyModule;

				var usesHierarchy = obj as IUsesHierarchyData;
				if (usesHierarchy != null)
					evrHierarchyModule.RemoveConsumer(usesHierarchy);
			}
		}
	}

}
