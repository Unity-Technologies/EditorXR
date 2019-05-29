namespace UnityEditor.Experimental.EditorVR.Core
{
    static class ModuleOrders
    {
        const int k_DefaultOrder = int.MaxValue / 2;

        public const int InterfaceModule = EditorVRLoadOrder - 1;
        public const int EditorVRLoadOrder = -int.MaxValue / 2;
        public const int MenuModuleLoadOrder = k_DefaultOrder;
        public const int SpatialHintModuleLoadOrder = k_DefaultOrder;
    }
}
