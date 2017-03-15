using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class SelectionModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrSelectionModule = evr.m_SelectionModule;

				var selectionChanged = obj as ISelectionChanged;
				if (selectionChanged != null)
					evr.m_SelectionChanged += selectionChanged.OnSelectionChanged;

				var selectObject = obj as ISelectObject;
				if (selectObject != null)
				{
					selectObject.getSelectionCandidate = evrSelectionModule.GetSelectionCandidate;
					selectObject.selectObject = evrSelectionModule.SelectObject;
				}
			}

			public void DisconnectInterface(object obj)
			{
				var selectionChanged = obj as ISelectionChanged;
				if (selectionChanged != null)
					evr.m_SelectionChanged -= selectionChanged.OnSelectionChanged;
			}
		}
	}

}
