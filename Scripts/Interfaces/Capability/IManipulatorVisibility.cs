#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provide access to the system to show or hide manipulator(s) on this tool / workspace / etc.
	/// </summary>
	public interface IManipulatorVisibility
	{
		/// <summary>
		/// Show or hide the manipulator(s)
		/// </summary>
		bool manipulatorVisible { set; }
	}
}
#endif
