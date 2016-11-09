using System;
using System.Collections.Generic;

public interface IFilterUI
{
	/// <summary>
	/// Set accessor for the filter list
	/// </summary>
	List<string> filterList { set; }
}