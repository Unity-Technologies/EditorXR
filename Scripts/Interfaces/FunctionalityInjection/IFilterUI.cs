#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors receive a filtered list of types found
	/// </summary>
	public interface IFilterUI
	{
		/// <summary>
		/// The filter list provided
		/// </summary>
		List<string> filterList { set; }

		/// <summary>
		/// The search query to be performed
		/// </summary>
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
