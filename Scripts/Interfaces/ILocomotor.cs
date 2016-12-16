namespace UnityEngine.Experimental.EditorVR.Tools
{
	/// <summary>
	/// Decorates a class as a locomotion implementer that uses the Viewer Pivot
	/// </summary>
	public interface ILocomotor : IUsesViewerPivot
	{
		/// <summary>
		/// Do not allow joystick locomotion until the joystick resets, in case some other system
		/// was using it, but had its ActionMapInput deactivated
		/// </summary>
		bool waitForReset { set; }
	}
}