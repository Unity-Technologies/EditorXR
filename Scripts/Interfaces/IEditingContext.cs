using System;
using UnityEngine;


namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implement this interface on a MonoBehavior to create an Editing Context.  Which is a blank space in which to author VR Editing tools.
	/// 
	/// Editing Contexts are applied as a stack.  Which means a context can be subverted by a newer context pushed to the top
	/// of the stack.  When a context pops itself from the stack, the next context on the stack, which had been subverted, is
	/// revived.
	/// </summary>
	public interface IEditingContext
	{
		/// <summary>
		/// Execute cleanup before this context is subverted in favor of a subcontext.  You can assume the context will be revived before it is destroyed.
		/// </summary>
		void OnSuspendContext();

		/// <summary>
		/// Undo whatever was cleaned or is needed to revive.  You can assume this context was previously subverted.
		/// </summary>
		void OnResumeContext();

		void Setup();

		void Dispose();
	}
}
