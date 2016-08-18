using UnityEngine.UI;

namespace UnityEngine.VR.Tools
{
	public interface IAction
	{
        /// <summary>
        /// The icon representing this Action that will be displayed in menus
        /// </summary>
	    Sprite icon { get; set; }

		/// <summary>
		/// Execute this action
		/// </summary>
		void Execute();
	}
}