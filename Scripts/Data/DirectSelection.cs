using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Stores the state of a direct selection
	/// </summary>
	public class DirectSelection
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