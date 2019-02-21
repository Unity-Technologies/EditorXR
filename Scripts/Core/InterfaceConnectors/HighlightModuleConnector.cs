using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class HighlightModuleConnector : Nested, IInterfaceConnector, ILateBindInterfaceMethods<HighlightModule>
        {
            public void LateBindInterfaceMethods(HighlightModule provider)
            {
                ISetHighlightMethods.setHighlight = provider.SetHighlight;
                ISetHighlightMethods.setBlinkingHighlight = provider.SetBlinkingHighlight;
            }

            public void ConnectInterface(object target, object userData = null)
            {
                var customHighlight = target as ICustomHighlight;
                if (customHighlight != null)
                    evr.GetModule<HighlightModule>().customHighlight += customHighlight.OnHighlight;
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var customHighlight = target as ICustomHighlight;
                if (customHighlight != null)
                    evr.GetModule<HighlightModule>().customHighlight -= customHighlight.OnHighlight;
            }
        }
    }
}
