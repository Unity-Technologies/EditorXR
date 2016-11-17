using System;
using UnityEngine;

/// <summary>
/// Make use of the spatial hash
/// </summary>
public interface IUsesSpatialHash
{
	/// <summary>
	/// Add all renderers of a GameObject (and its children) to the spatial hash for queries, direct selection, etc.
	/// </summary>
	Action<GameObject> addToSpatialHash { set; }

	/// <summary>
	/// Remove all renderers of a GameObject (and its children) from the spatial hash
	/// </summary>
	Action<GameObject> removeFromSpatialHash { set; }
}