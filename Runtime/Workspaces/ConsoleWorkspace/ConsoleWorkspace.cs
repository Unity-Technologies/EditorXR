namespace Unity.EditorXR.Workspaces
{
#if UNITY_EDITOR
    [MainMenuItem("Console", "Workspaces", "View errors, warnings and other messages")]
    [SpatialMenuItem("Console", "Workspaces", "View errors, warnings and other messages")]
#else
    [EditorOnlyWorkspace]
#endif
    sealed class ConsoleWorkspace : EditorWindowWorkspace
    {
    }
}
