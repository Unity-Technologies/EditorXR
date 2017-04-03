#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class HighlightModuleConnector : Nested, IInterfaceConnector, ILateBindInterfaceMethods<HighlightModule>
		{
			public void LateBindInterfaceMethods(HighlightModule provider)
			{
				ISetHighlightMethods.setHighlight = provider.SetHighlight;
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var customHighlight = obj as ICustomHighlight;
				if (customHighlight != null)
				{
					var evrHighlightModule = evr.GetModule<HighlightModule>();
					evrHighlightModule.customHighlight += customHighlight.OnHighlight;
				}
			}

			public void DisconnectInterface(object obj)
			{
				var customHighlight = obj as ICustomHighlight;
				if (customHighlight != null)
				{
					var evrHighlightModule = evr.GetModule<HighlightModule>();
					evrHighlightModule.customHighlight -= customHighlight.OnHighlight;
				}
			}
		}
	}
}
#endif
