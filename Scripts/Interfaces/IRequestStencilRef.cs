using System;

namespace UnityEngine.Experimental.EditorVR.Tools
{
	/// <summary>
	/// Provide the ability to request a new unique stencil ref value
	/// </summary>
	public interface IRequestStencilRef
	{
		/// <summary>
		/// Get a new unique stencil ref value
		/// </summary>
		Func<byte> requestStencilRef { set; }
	}
}
