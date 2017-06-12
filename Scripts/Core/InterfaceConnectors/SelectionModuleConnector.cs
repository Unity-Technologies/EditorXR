#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class SelectionModuleConnector : Nested, IInterfaceConnector, ILateBindInterfaceMethods<SelectionModule>
		{
			public void LateBindInterfaceMethods(SelectionModule provider)
			{
				ISelectObjectMethods.getSelectionCandidate = provider.GetSelectionCandidate;
				ISelectObjectMethods.selectObject = provider.SelectObject;
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var selectionChanged = obj as ISelectionChanged;
				if (selectionChanged != null)
					evr.selectionChanged += selectionChanged.OnSelectionChanged;
			}

			public void DisconnectInterface(object obj)
			{
				var selectionChanged = obj as ISelectionChanged;
				if (selectionChanged != null)
					evr.selectionChanged -= selectionChanged.OnSelectionChanged;
			}
		}
	}
}
#endif
