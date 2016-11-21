using System;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Decorates tools which supply their own (singular) ActionMap
	/// </summary>
	public interface ICustomActionMap
	{
		/// <summary>
		/// Provides access to the custom action map
		/// </summary>
		ActionMap actionMap { get; }

		/// <summary>
		/// Provides access to the ActionMapInput created using actionMap
		/// </summary>
		ActionMapInput actionMapInput { set; get; }

		void ProcessInput(Action<InputControl> consumeControl);
	}
}