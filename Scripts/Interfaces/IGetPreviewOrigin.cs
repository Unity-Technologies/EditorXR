using System;

namespace UnityEngine.VR.Modules
{
	public interface IGetPreviewOrigin
	{
		/// <summary>
		/// Get the preview transform attached to the given rayOrigin
		/// </summary>
		Func<Transform, Transform> getPreviewOriginForRayOrigin { set; }
	}
}