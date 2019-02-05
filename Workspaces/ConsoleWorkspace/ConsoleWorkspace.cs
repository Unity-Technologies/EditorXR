#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    [MainMenuItem("Console", "Workspaces", "View errors, warnings and other messages")]
    [SpatialMenuItem("Console", "Workspaces", "View errors, warnings and other messages")]
    sealed class ConsoleWorkspace : EditorWindowWorkspace
    {
    }
}
#endif
