/// <summary>
/// Decorates types which need to respond to a change in selection
/// </summary>
public interface ISelectionChanged
{
	/// <summary>
	/// Called when selection changes (via Selection.onSelectionChange subscriber)
	/// </summary>
	void OnSelectionChanged();
}