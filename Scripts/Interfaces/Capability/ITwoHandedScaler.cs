#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides methods and delegates used to directly select and grab scene objects
	/// </summary>
	public interface ITwoHandedScaler
	{
		/// <summary>
		/// Returns whether the given ray origin is involved in two-handed scaling
		/// </summary>
		/// <param name="rayOrigin">The ray origin to check</param>
		/// <returns></returns>
		bool IsTwoHandedScaling(Transform rayOrigin);
	}
}
#endif
