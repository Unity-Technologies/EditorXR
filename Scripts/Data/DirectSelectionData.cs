using UnityEngine;
using UnityEditor.Experimental.EditorVR;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Stores the state of a direct selection
	/// </summary>
	public sealed class DirectSelectionData
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