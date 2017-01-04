namespace UnityEngine.Experimental.EditorVR.UI
{
	/// <summary>
	/// Allows fine-grained control of what constitutes a selection
	/// </summary>
	public interface ISelectionFlags
	{
		/// <summary>
		/// Flags to control selection
		/// </summary>
		SelectionFlags selectionFlags { get; set; }
	}
}