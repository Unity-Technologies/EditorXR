using System;

namespace UnityEngine.Experimental.EditorVR.Tools
{
	/// <summary>
	/// Decorates objects which can delete objects from the scene
	/// </summary>
	public interface IDeleteSceneObject
	{
		/// <summary>
		/// Destroy the given game object and remove it from the spatial hash
		/// </summary>
		Action<GameObject> deleteSceneObject { set; }
	}
}