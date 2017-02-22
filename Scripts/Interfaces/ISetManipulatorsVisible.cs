using System;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provide access to show or hide manipulator(s)
	/// </summary>
	public interface ISetManipulatorsVisible
	{
		/// <summary>
		/// Show or hide the manipulator(s)
		/// </summary>
		Action<ISetManipulatorsVisible, bool> setManipulatorsVisible { set; }
	}
}