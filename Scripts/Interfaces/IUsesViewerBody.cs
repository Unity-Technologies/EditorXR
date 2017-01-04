using System;

namespace UnityEngine.Experimental.EditorVR.Tools
{
	/// <summary>
	/// Provides access to checks that can test against the viewer's body
	/// </summary>
	public interface IUsesViewerBody
	{
		/// <summary>
		/// Returns whether the specified transform is over the viewer's shoulders and behind the head
		/// </summary>
		Func<Transform, bool> isOverShoulder { set; }
	}
}