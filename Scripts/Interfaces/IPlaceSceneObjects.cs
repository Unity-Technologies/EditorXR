#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class the ability to place objects in the scene, or a MiniWorld
	/// </summary>
	public interface IPlaceSceneObjects
	{
	}

	public static class IPlaceSceneObjectsMethods
	{
		internal static Action<Transform[], Transform, Quaternion, float> placeSceneObjects { get; set; }

		/// <summary>
		/// Method used to place groups of objects in the scene/MiniWorld
		/// </summary>
		/// <param name="transforms">Transforms of the GameObjects to place</param>
		/// <param name="parent">Current parent of the group, if any</param>
		/// <param name="rotationOffset">rotation offset to apply to the group, if any</param>
		/// <param name="scaleFactor">Scale multiplier to apply to the group, if any</param>
		public static void PlaceSceneObject(this IPlaceSceneObjects obj, Transform[] transforms, Transform parent = null, Quaternion rotationOffset = default(Quaternion), float scaleFactor = 1)
		{
			placeSceneObjects(transforms, parent, rotationOffset, scaleFactor);
		}
	}
}
#endif
