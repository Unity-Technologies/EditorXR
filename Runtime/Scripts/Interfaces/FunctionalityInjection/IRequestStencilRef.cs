using System;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provide the ability to request a new unique stencil ref value
    /// </summary>
    public interface IRequestStencilRef
    {
    }

    public static class IRequestStencilRefMethods
    {
        internal static Func<byte> requestStencilRef { get; set; }

        /// <summary>
        /// Get a new unique stencil ref value
        /// </summary>
        public static byte RequestStencilRef(this IRequestStencilRef obj)
        {
            return requestStencilRef();
        }
    }
}
