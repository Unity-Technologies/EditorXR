using UnityEngine;

/// <summary>
/// For the purpose of interacting with MiniWorlds
/// </summary>
public interface IMiniWorld
{
	/// <summary>
	/// Gets the root transform of the miniWorld itself
	/// </summary>
	Transform miniWorldTransform { get; }

	/// <summary>
	/// Tests whether a point is contained within the actual miniWorld bounds (not the reference bounds)
	/// </summary>
	/// <param name="position">World space point to be tested</param>
	/// <returns>True if the point is contained</returns>
	bool Contains(Vector3 position);
	
	/// <summary>
	/// Gets the reference transform used to represent the origin and size of the space represented within the miniWorld
	/// </summary>
	Transform referenceTransform { get; }
}