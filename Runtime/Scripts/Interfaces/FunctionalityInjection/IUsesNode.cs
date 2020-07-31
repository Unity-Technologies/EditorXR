using Unity.EditorXR.Interfaces;

namespace Unity.EditorXR
{
    /// <summary>
    /// Adds Node information to determine which hand the tool is attached to
    /// </summary>
    public interface IUsesNode
    {
        /// <summary>
        /// The node associated with this tool
        /// </summary>
        Node node { set; }
    }
}
