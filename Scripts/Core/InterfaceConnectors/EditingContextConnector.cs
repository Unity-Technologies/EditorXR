#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class EditingContextConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var setEditingContext = obj as ISetEditingContext;
				if (setEditingContext != null)
					setEditingContext.allContexts = EditingContextLauncher.GetAllEditingContexts();
			}

			public void DisconnectInterface(object obj)
			{
			}

			public EditingContextConnector()
			{
				var launcher = EditingContextLauncher.s_Instance;
				ISetEditingContextMethods.setEditingContext = launcher.SetEditingContext;
				ISetEditingContextMethods.peekEditingContext = launcher.PeekEditingContext;
				ISetEditingContextMethods.pushEditingContext = launcher.PushEditingContext;
				ISetEditingContextMethods.popEditingContext = launcher.PopEditingContext;
			}
		}
	}
}
#endif
