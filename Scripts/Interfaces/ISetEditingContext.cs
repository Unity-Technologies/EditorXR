#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	public interface ISetEditingContext
	{
	}

	public static class ISetEditingContextMethods
	{
		internal static Func<List<IEditingContext>> getAvailableEditingContexts { get; set; }
		internal static Action<IEditingContext> setEditingContext { get; set; }
		internal static Action restorePreviousEditingContext { get; set; }

		/// <summary>
		/// Get the currently available editing contexts
		/// NOTE: Dynamic contexts can be added to the list to make them available
		/// </summary>
		/// <returns>List of the currently available editing contexts</returns>
		public static List<IEditingContext> GetAvailableEditingContexts(this ISetEditingContext obj)
		{
			return getAvailableEditingContexts();
		}

		/// <summary>
		/// Set the editing context, which will dispose of the current editing context
		/// </summary>
		/// <param name="context">The editing context to use</param>
		public static void SetEditingContext(this ISetEditingContext obj, IEditingContext context)
		{
			setEditingContext(context);
		}

		/// <summary>
		/// Restore the previous editing context, which will dispose of the current editing context
		/// </summary>
		public static void RestorePreviousEditingContext(this ISetEditingContext obj)
		{
			restorePreviousEditingContext();
		}
	}

}
#endif
