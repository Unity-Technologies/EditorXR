namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Designates a tool as a Transform tool and allows for control and state queries
	/// </summary>
	public interface ITransformTool
	{
		/// <summary>
		/// Get or set whether the tool can direclty manipulate objects using the pointers
		/// </summary>
		bool directManipulationEnabled { get; set; }

		/// <summary>
		/// Drop a currently held object
		/// </summary>
		/// <param name="obj">The object we wish to drop</param>
		void DropHeldObject(Transform obj);
		
		/// <summary>
		/// Get the object held by a given rayOrign
		/// </summary>
		/// <param name="rayOrigin">The rayOrigin to query</param>
		/// <returns></returns>
		Transform GetHeldObject(Transform rayOrigin);
	}
}