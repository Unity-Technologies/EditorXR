using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class MultipleRayInputModuleConnector : Nested, ILateBindInterfaceMethods<MultipleRayInputModule>
		{
			public void LateBindInterfaceMethods(MultipleRayInputModule provider)
			{
				IIsHoveringOverUIMethods.isHoveringOverUI = provider.IsHoveringOverUI;
			}
		}
	}
}
