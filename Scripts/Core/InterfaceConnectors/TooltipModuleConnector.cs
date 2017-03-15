using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class TooltipModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var tooltipModule = evr.m_TooltipModule;

				var usesTooltip = obj as ISetTooltipVisibility;
				if (usesTooltip != null)
				{
					usesTooltip.showTooltip = tooltipModule.ShowTooltip;
					usesTooltip.hideTooltip = tooltipModule.HideTooltip;
				}
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}

}
