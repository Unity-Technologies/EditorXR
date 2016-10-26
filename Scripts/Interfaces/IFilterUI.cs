using System.Collections.Generic;

public interface IFilterUI
{
	/// <summary>
	/// Set the filter list
	/// </summary>
	/// <param name="filters">The filter list</param>
	void SetFilters(List<string> filters);
}