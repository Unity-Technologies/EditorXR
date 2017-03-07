#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides access to other tools of the same type
	/// </summary>
	public interface ILinkedObject
	{
		/// <summary>
		/// List of other tools of the same type (not including this one)
		/// </summary>
		List<ILinkedObject> linkedObjects { set; }

		/// <summary>
		/// Whether this is the primary tool (the first to be created, can be either hand)
		/// </summary>
		Func<ILinkedObject, bool> isSharedUpdater { set; }
	}
}
#endif
