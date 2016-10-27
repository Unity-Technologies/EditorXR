using System;
using System.Collections.Generic;

public interface IFilterUI
{
	/// <summary>
	/// Set accessor for the filter list
	/// </summary>
	List<string> filterList { set; }

	/// <summary>
	/// Supplied by ConnectInterfaces to allow getting the current available filter list
	/// </summary>
	Func<List<string>> getFilterList { set; }
}