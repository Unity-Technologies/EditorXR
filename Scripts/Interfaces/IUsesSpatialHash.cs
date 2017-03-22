#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Make use of the spatial hash
	/// </summary>
	public interface IUsesSpatialHash
	{
	}

	public static class IUsesSpatialHashMethods
	{
		internal static Action<GameObject> addToSpatialHash { get; set; }
		internal static Action<GameObject> removeFromSpatialHash { get; set; }

		/// <summary>
		/// Add all renderers of a GameObject (and its children) to the spatial hash for queries, direct selection, etc.
		/// </summary>
		public static void AddToSpatialHash(this IUsesSpatialHash obj, GameObject go)
		{
			if (addToSpatialHash != null)
				addToSpatialHash(go);
		}

		/// <summary>
		/// Remove all renderers of a GameObject (and its children) from the spatial hash
		/// </summary>
		public static void RemoveFromSpatialHash(this IUsesSpatialHash obj, GameObject go)
		{
			if (removeFromSpatialHash != null)
				removeFromSpatialHash(go);
		}
	}
}
#endif
