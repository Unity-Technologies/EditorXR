#if UNITY_EDITOR
using UnityEngine;
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
		public Node node { get; set; }

		/// <summary>
		/// The object which is selected
		/// </summary>
		public GameObject gameObject { get; set; }

		/// <summary>
		/// The input which is associated with the rayOrigin
		/// </summary>
		public ActionMapInput input { get; set; }
	}
}
#endif
