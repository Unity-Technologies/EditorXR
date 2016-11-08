using System;
using System.Collections.Generic;
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

	/// <summary>
	/// Preprocessing event that returns true if the MiniWorld should render
	/// </summary>
	Func<IMiniWorld, bool> preProcessRender { set; }

	/// <summary>
	/// Postprocessing event to clean up after render
	/// </summary>
	Action<IMiniWorld> postProcessRender { set; }

	/// <summary>
	/// The combined scale of the MiniWorld and its reference transform
	/// </summary>
	Vector3 miniWorldScale { get; }

	/// <summary>
	/// Sets a list of renderers to be skipped when rendering the MiniWorld
	/// </summary>
	List<Renderer> ignoreList { set; }
}