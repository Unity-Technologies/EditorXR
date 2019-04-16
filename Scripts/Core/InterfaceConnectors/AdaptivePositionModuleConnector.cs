#if UNITY_2018_3_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class AdaptivePositionModuleConnector : Nested, IInterfaceConnector
        {
            public void ConnectInterface(object target, object userData = null)
            {
                var adaptsPosition = target as IAdaptPosition;
                if (adaptsPosition != null)
                {
                    var adaptivePositionModule = evr.GetModule<AdaptivePositionModule>();
                    adaptivePositionModule.ControlObject(adaptsPosition);
                }
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var adaptsPosition = target as IAdaptPosition;
                if (adaptsPosition != null)
                {
                    var adaptivePositionModule = evr.GetModule<AdaptivePositionModule>();
                    adaptivePositionModule.FreeObject(adaptsPosition);
                }
            }
        }
    }
}
#endif
