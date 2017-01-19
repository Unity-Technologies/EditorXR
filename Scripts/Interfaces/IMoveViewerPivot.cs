namespace UnityEngine.Experimental.EditorVR
{
	/// <summary>
	/// Method signature for moving the viewer pivot
	/// </summary>
	/// <param name="position">Target position</param>
	/// <param name="viewDirection">Target view direction in the XZ plane. Y component will be ignored</param>
	public delegate void MoveViewerPivotDelegate(Vector3 position, Vector3? viewDirection = null);

	/// <summary>
	/// Decorates types that need to move the viewer pivot
	/// </summary>
	public interface IMoveViewerPivot
	{
		/// <summary>
		/// Move the viewer pivot using the standard interpolation provided by the system
		/// </summary>
		MoveViewerPivotDelegate moveViewerPivot { set; }
	}
}