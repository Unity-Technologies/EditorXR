
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Deliver a stencil ref value for use in materials (useful for masks)
    /// </summary>
    public interface IUsesStencilRef
    {
        /// <summary>
        /// The stencil reference value
        /// </summary>
        byte stencilRef { get; set; }
    }
}

