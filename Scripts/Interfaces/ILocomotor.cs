namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Decorates a class as a locomotion implementer
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