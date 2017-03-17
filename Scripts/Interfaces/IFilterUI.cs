#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors receive a list of asset types found in the project
	/// </summary>
	public interface IFilterUI
	{
		/// <summary>
		/// Set accessor for the filter list
		/// </summary>
		List<string> filterList { set; }
		string searchQuery { get; }
	}

	public static class IFilterUIMethods
	{
		public static bool MatchesFilter(this IFilterUI filterUI, string type)
		{
			var pieces = filterUI.searchQuery.Split(':');
			if (pieces.Length > 1)
			{
				if (pieces[1].StartsWith(type))
					return true;
			}
			else
			{
				return true;
			}

			return false;
		}
	}
}
#endif
