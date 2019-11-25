using Unity.Labs.EditorXR.Interfaces;

namespace Unity.Labs.EditorXR
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
