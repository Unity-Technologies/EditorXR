namespace UnityEditor.Experimental.EditorVR.Core
{
    static class ModuleOrders
    {
        const int k_DefaultEarlyOrder = int.MinValue / 2;
        const int k_DefaultLateOrder = int.MaxValue / 2;

        public const int InterfaceModule = EditorVRLoadOrder - 1;
        public const int EditorVRLoadOrder = k_DefaultEarlyOrder;
        public const int DeviceInputModuleOrder = k_DefaultEarlyOrder;
        public const int MenuModuleLoadOrder = k_DefaultLateOrder;
        public const int SpatialHintModuleLoadOrder = k_DefaultLateOrder;

        public const int DirectSelectionModuleBehaviorOrder = k_DefaultLateOrder;
        public const int DeviceInputModuleBehaviorOrder = DirectSelectionModuleBehaviorOrder + 1;
        public const int MenuModuleBehaviorOrder = DeviceInputModuleBehaviorOrder + 1;
        public const int UIModuleBehaviorOrder = DeviceInputModuleBehaviorOrder + 1;
        public const int HighlightModuleBehaviorOrder = DeviceInputModuleBehaviorOrder + 1;
    }
}
