#if UNITY_EDITOR

namespace UnityEditor.Experimental.EditorVR
{
    public interface ICustomAlternateMenu : IMenu
    {
        int menuPriority { get; }
    }
}
#endif
