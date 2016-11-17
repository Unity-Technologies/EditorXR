using UnityEngine;

public interface IVacuumable
{
	/// <summary>
	/// Bounding volume to test raycast
	/// </summary>
	Bounds vacuumBounds { get; }

	/// <summary>
	/// Does not require implementation unless implementing class is not a MonoBehaviour
	/// </summary>
	Transform transform { get; }
}