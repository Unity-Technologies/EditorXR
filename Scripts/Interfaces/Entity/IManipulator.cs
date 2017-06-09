#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	[Flags]
	public enum ConstrainedAxis
	{
		X = 1 << 0,
		Y = 1 << 1,
		Z = 1 << 2
	}

	/// <summary>
	/// Gives decorated class access to the GameObject over which a particular ray is hovering
	/// </summary>
	public interface IManipulator
	{
		/// <summary>
		/// Delegate that processes the translation, using the vector3 passed in
		/// Caller also provides the ray origin that is doing the action whether or not this translation is axis-constrained, and whether to apply snapping
		/// </summary>
		Func<Vector3, Transform, ConstrainedAxis, bool, bool> translate { set; }

		/// <summary>
		/// Delegate that processes the rotation, using the quaternion passed in
		/// </summary>
		Action<Quaternion> rotate { set; }

		/// <summary>
		/// Delegate that processes the scale, using the vector3 passed in
		/// </summary>
		Action<Vector3> scale { set; }

		/// <summary>
		/// Delegate that is called once after every drag starts
		/// </summary>
		event Action dragStarted;

		/// <summary>
		/// Delegate that is called once after every drag ends
		/// </summary>
		event Action<Transform> dragEnded;

		/// <summary>
		/// Bool denoting the drag-state of a manipulator that implements this interface
		/// </summary>
		bool dragging { get; }
	}
}
#endif
