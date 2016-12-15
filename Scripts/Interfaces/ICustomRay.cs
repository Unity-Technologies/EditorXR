namespace UnityEngine.Experimental.EditorVR.Tools
{
	/// <summary>
	/// Method signature for hiding or showing the default ray
	/// <param name="rayOrigin">The ray to hide or show</param>
	/// <param name="onlyRay">An optional parameter to hide or show only the ray</param>
	/// </summary>
	public delegate void DefaultRayVisibilityDelegate(Transform rayOrigin, bool onlyRay = false);

	/// <summary>
	/// Implementors can show & hide the default ray
	/// </summary>
	public interface ICustomRay : IRayLocking
	{
		/// <summary>
		/// Show the default proxy ray/cone
		/// </summary>
		DefaultRayVisibilityDelegate showDefaultRay { set; }

		/// <summary>
		/// Hide the default proxy ray/cone
		/// </summary>
		DefaultRayVisibilityDelegate hideDefaultRay { set; }
	}
}