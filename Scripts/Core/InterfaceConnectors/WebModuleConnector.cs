#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class WebModuleConnector : Nested, ILateBindInterfaceMethods<WebModule>
		{
			public void LateBindInterfaceMethods(WebModule provider)
			{
				IWebMethods.download = provider.Download;
			}
		}
	}
}
#endif
