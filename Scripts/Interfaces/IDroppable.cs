namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Implementors can be dropped on IDropReceivers
	/// </summary>
	public interface IDroppable
	{
		/// <summary>
		/// Get the underlying object that will be dropped
		/// </summary>
		object GetDropObject();
	}
}