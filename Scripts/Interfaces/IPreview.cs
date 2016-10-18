using System;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Positions a preview object above the rayOrigin
	/// </summary>
	/// <param name="preview">The Transform we are positioning</param>
	/// <param name="rayOrigin">The rayOrigin to which we attach the preview</param>
	/// <param name="t">Optional interpolation parameter for smooth transitions</param>
	/// <param name="localRotation">Optional local rotation to apply</param>
	public delegate void PreviewDelegate(Transform preview, Transform rayOrigin, float t = 1f, Quaternion? localRotation = null);

	public interface IPreview
	{
		/// <summary>
		/// Position a preview object above the rayOrigin
		/// </summary>
		PreviewDelegate preview { set; }

		/// <summary>
		/// Get the preview transform attached to the given rayOrigin
		/// </summary>
		Func<Transform, Transform> getPreviewOriginForRayOrigin { set; }
	}
}