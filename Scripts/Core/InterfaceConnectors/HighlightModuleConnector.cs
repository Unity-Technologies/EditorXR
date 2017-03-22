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
				var evrHighlightModule = evr.m_HighlightModule;

				var customHighlight = obj as ICustomHighlight;
				if (customHighlight != null)
					evrHighlightModule.customHighlight += customHighlight.OnHighlight;
			}

			public void DisconnectInterface(object obj)
			{
				var evrHighlightModule = evr.m_HighlightModule;

				var customHighlight = obj as ICustomHighlight;
				if (customHighlight != null)
					evrHighlightModule.customHighlight -= customHighlight.OnHighlight;
			}
		}
	}
}
