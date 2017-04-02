#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Declares a class as a Workspace within the system
	/// </summary>
	public interface IWorkspace : IVacuumable, ICustomActionMap
	{
		/// <summary>
		/// First-time setup; will be called after Awake and ConnectInterfaces
		/// </summary>
		void Setup();

		/// <summary>
		/// Call this in OnDestroy to inform the system
		/// </summary>
		event Action<IWorkspace> destroyed;

		/// <summary>
		/// Bounding box for entire workspace, including UI handles
		/// </summary>
		Bounds outerBounds { get; }

		Transform leftRayOrigin { set; }
		Transform rightRayOrigin { set; }
		Func<Transform, float> getPointerLength { set; }
	}
}
#endif
