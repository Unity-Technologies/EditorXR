#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Declares a class as one which contains data relevant to display in the Spatial Menu system
    /// </summary>
    public interface ISpatialMenuData
    {
        string spatialMenuDescription { get; }
    }
}
#endif