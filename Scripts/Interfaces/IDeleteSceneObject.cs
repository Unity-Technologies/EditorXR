#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Decorates objects which can delete objects from the scene
	/// </summary>
	public interface IDeleteSceneObject
	{
	}

	public static class IDeleteSceneObjectMethods
	{
		internal static Action<GameObject> deleteSceneObject { get; set; }

		/// <summary>
		/// Destroy the given game object and remove it from the spatial hash
		/// </summary>
		public static void DeleteSceneObject(this IDeleteSceneObject obj, GameObject go)
		{
			if (deleteSceneObject!= null)
				deleteSceneObject(go);
		}
	}
}
#endif
