using System.Collections.Generic;

public interface IUsesHierarchyData
{
	/// <summary>
	/// Set accessor for hierarchy list data
	/// Used to update existing implementors after lazy load completes
	/// </summary>
	List<HierarchyData> hierarchyData { set; }
}