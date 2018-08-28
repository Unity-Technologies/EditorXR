#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Declares that the Spatial Menu should display a custom alternate description for this object
    /// </summary>
    internal interface ISpatialMenuCustomDescription
    {
        string spatialMenuCustomDescription { get; }
    }
}
#endif