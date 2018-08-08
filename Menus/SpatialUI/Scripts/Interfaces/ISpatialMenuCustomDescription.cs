#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Declares that the Spatial Menu should display a custom alternate description for this object
    /// </summary>
    public interface ISpatialMenuCustomDescription
    {
        string spatialMenuCustomDescription { get; }
    }
}
#endif