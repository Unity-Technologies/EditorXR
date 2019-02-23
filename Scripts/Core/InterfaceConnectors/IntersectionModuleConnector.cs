using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class IntersectionModuleConnector : Nested, IInterfaceConnector, ILateBindInterfaceMethods<IntersectionModule>
        {
            public void LateBindInterfaceMethods(IntersectionModule provider)
            {
                IRaycastMethods.raycast = provider.Raycast;
                ICheckBoundsMethods.checkBounds = provider.CheckBounds;
                ICheckSphereMethods.checkSphere = provider.CheckSphere;
                IContainsVRPlayerCompletelyMethods.containsVRPlayerCompletely = provider.ContainsVRPlayerCompletely;
            }

            public void ConnectInterface(object target, object userData = null)
            {
                var standardIgnoreList = target as IStandardIgnoreList;
                if (standardIgnoreList != null)
                {
                    var intersectionModule = evr.GetModule<IntersectionModule>();
                    standardIgnoreList.ignoreList = intersectionModule.standardIgnoreList;
                }
            }

            public void DisconnectInterface(object target, object userData = null) { }
        }
    }
}
