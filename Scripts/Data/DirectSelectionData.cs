using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Stores the state of a direct selection
	/// </summary>
	public class DirectSelectionData
	{
		/// <summary>
		/// The Node used to select the object
		/// </summary>
		public Node node;

		/// <summary>
		/// The object which is selected
		/// </summary>
		public GameObject gameObject;

		/// <summary>
		/// The input which is associated with the rayOrigin
		/// </summary>
		public ActionMapInput input;
	}
}