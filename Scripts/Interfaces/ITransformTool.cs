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

		/// <summary>
		/// Transfer a held object between rayOrigins (i.e. dragging into the MiniWorld)
		/// </summary>
		/// <param name="rayOrigin">rayOrigin of current held object</param>
		/// <param name="destRayOrigin">Destination rayOrigin</param>
		/// <param name="deltaOffset">Change in position offset (added to GrabData.positionOffset)</param>
		void TransferHeldObject(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset);
	}
}