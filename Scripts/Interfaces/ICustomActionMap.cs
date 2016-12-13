using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Decorates tools which supply their own (singular) ActionMap
	/// </summary>
	public interface ICustomActionMap : IProcessInput
	{
		/// <summary>
		/// Provides access to the custom action map
		/// </summary>
		ActionMap actionMap { get; }
	}
}