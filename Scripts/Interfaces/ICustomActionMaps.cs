using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Decorates tools which supply their own (singular) ActionMap
	/// </summary>
	public interface ICustomActionMaps
	{
		/// <summary>
		/// Provides access to the custom action maps
		/// </summary>
		ActionMap[] actionMaps { get; }

		/// <summary>
		/// Provides access to the ActionMapInput created using actionMap
		/// </summary>
		ActionMapInput[] actionMapInputs { set; get; }
	}
}