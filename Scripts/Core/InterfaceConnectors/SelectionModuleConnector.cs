#if UNITY_EDITOR && UNITY_EDITORVR

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class SelectionModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object @object, object userData = null)
			{
				var selectionChanged = @object as ISelectionChanged;
				if (selectionChanged != null)
					evr.selectionChanged += selectionChanged.OnSelectionChanged;
			}

			public void DisconnectInterface(object @object, object userData = null)
			{
				var selectionChanged = @object as ISelectionChanged;
				if (selectionChanged != null)
					evr.selectionChanged -= selectionChanged.OnSelectionChanged;
			}
		}
	}
}
#endif
