using UnityEngine.InputNew;

namespace UnityEngine.Experimental.EditorVR.Tools
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