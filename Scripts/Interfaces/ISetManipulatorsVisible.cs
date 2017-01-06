using System;

namespace UnityEngine.Experimental.EditorVR
{
	/// <summary>
	/// Provide access to show or hide manipulator(s)
	/// </summary>
	public interface ISetManipulatorsVisible
	{
		/// <summary>
		/// Show or hide the manipulator(s)
		/// </summary>
		Action<bool> setManipulatorsVisible { set; }
	}
}