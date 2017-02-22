namespace UnityEditor.Experimental.EditorVR.UI
{
	/// <summary>
	/// Allows fine-grained control of what constitutes a selection
	/// </summary>
	internal interface ISelectionFlags
	{
		/// <summary>
		/// Flags to control selection
		/// </summary>
		SelectionFlags selectionFlags { get; set; }
	}
}