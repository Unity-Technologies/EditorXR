#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provide access to show or hide manipulator(s)
	/// </summary>
	public interface ISetManipulatorsVisible
	{
	}

	public static class ISetManipulatorsVisibleMethods
	{
		internal static Action<ISetManipulatorsVisible, bool> setManipulatorsVisible { get; set; }

		/// <summary>
		/// Show or hide the manipulator(s)
		/// </summary>
		public static void SetManipulatorsVisible(this ISetManipulatorsVisible obj, ISetManipulatorsVisible requester, bool visibility)
		{
			if (setManipulatorsVisible != null)
				setManipulatorsVisible(requester, visibility);
		}
	}
}
#endif
