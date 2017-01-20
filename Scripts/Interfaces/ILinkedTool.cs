using System.Collections.Generic;

/// <summary>
/// Provides access to other tools of the same type
/// </summary>
public interface ILinkedTool
{
	/// <summary>
	/// List of other tools of the same type (not including this one)
	/// </summary>
	List<ILinkedTool> otherTools { get; }

	/// <summary>
	/// Whether this is the primary tool (the first to be created, can be either hand)
	/// </summary>
	bool primary { get; set; }
}
