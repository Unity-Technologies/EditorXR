using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class HighlightModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrHighlightModule = evr.m_HighlightModule;

				var highlight = obj as ISetHighlight;
				if (highlight != null)
					highlight.setHighlight = evrHighlightModule.SetHighlight;
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}

}
