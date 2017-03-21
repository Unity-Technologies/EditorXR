using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class MultipleRayInputModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrInputModule = evr.m_InputModule;

				var isHoveringOverUI = obj as IIsHoveringOverUI;
				if (isHoveringOverUI != null)
					isHoveringOverUI.isHoveringOverUI = evrInputModule.IsHoveringOverUI;
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}

}
