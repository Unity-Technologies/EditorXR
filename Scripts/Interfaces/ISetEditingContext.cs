#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	public interface ISetEditingContext
	{
		/// <summary>
		/// All of the available editing contexts
		/// </summary>
		List<IEditingContext> allContexts { set; }
	}

	public static class ISetEditingContextMethods
	{
		internal delegate bool PushEditingContextDelegate(IEditingContext context, out string errorMessage);

		internal static Action<IEditingContext> setEditingContext { get; set; }
		internal static Func<IEditingContext> peekEditingContext { get; set; }
		internal static PushEditingContextDelegate pushEditingContext { get; set; }
		internal static Action popEditingContext { get; set; }
	
		/// <summary>
		/// Set the editing context, which will pop all other editing contexts
		/// </summary>
		/// <param name="context">The editing context to use</param>
		public static void SetEditingContext(this ISetEditingContext obj, IEditingContext context)
		{
			setEditingContext(context);
		}

		/// <summary>
		/// Peek at the current editing context
		/// </summary>
		/// <returns>The current editing context</returns>
		public static IEditingContext PeekEditingContext(this ISetEditingContext obj)
		{
			return peekEditingContext();
		}

		/// <summary>
		/// Push an editing context
		/// </summary>
		/// <param name="context">The editing context to activate</param>
		public static bool PushEditingContext(this ISetEditingContext obj, IEditingContext context, out string errorMessage)
		{
			return pushEditingContext(context, out errorMessage);
		}

		/// <summary>
		/// Pop the current editing context
		/// </summary>
		public static void PopEditingContext(this ISetEditingContext obj)
		{
			popEditingContext();
		}
	}

}
#endif
