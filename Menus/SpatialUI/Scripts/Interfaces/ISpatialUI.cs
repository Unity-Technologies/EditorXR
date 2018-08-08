#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Declares that the implementer includes core SpatialUI implementation
    /// The SpatialMenu is the first robust implementation of and ISpatialUI
    /// </summary>
    internal interface ISpatialUI
    {
        // Add core Spatial UI layer here

        SpatialUIToggle m_SpatialPinToggle { get; set; }
    }
}
#endif