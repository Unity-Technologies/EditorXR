using UnityEngine;

/// <summary>
/// For the purpose of transforming operations in to MiniWorld space
/// </summary>
public interface IMiniWorld
{
	/// <summary>
	/// Gets the transform parent of the miniWorld itself
	/// </summary>
	Transform miniWorldTransform { get; }

	/// <summary>
	/// Returns true if a point is contained within the actual miniWorld bounds (not the reference bounds)
	/// </summary>
	/// <param name="position">World space point to be tested</param>
	/// <returns></returns>
	bool Contains(Vector3 position);
	
	/// <summary>
	/// Gets the reference transform used to represent the origin and size of the space represented within the miniWorld
	/// </summary>
	Transform referenceTransform { get; }
}