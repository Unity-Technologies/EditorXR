#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Method signature for moving the camera rig
	/// </summary>
	/// <param name="position">Target position</param>
	/// <param name="viewDirection">Target view direction in the XZ plane. Y component will be ignored</param>
	public delegate void MoveCameraRigDelegate(Vector3 position, Vector3? viewDirection = null);

	/// <summary>
	/// Decorates types that need to move the camera rig
	/// </summary>
	public interface IMoveCameraRig
	{
		/// <summary>
		/// Move the camera rig using the standard interpolation provided by the system
		/// </summary>
		MoveCameraRigDelegate moveCameraRig { set; }
	}
}
#endif
