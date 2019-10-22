#if ENABLE_EDITORXR
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
                IWebMethods.downloadTexture = provider.DownloadTexture;
                IWebMethods.downloadToDisk = provider.Download;
            }
        }
    }
}
#endif
