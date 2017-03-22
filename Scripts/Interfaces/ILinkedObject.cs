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
	}

	public static class ILinkedObjectMethods
	{
		internal static Func<ILinkedObject, bool> isSharedUpdater { get; set; }

		/// <summary>
		/// Returns whether the specified ray origin is hovering over a UI element
		/// </summary>
		public static bool IsSharedUpdater(this ILinkedObject obj, ILinkedObject linkedObject)
		{
			if (isSharedUpdater != null)
				return isSharedUpdater(linkedObject);

			return false;
		}
	}
}
#endif
