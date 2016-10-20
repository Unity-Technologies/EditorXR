using System;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Decorates objects which can check if a rayOrigin is over the user's shoulder or behind their head
	/// </summary>
	public interface IOverShoulderCheck
	{
		/// <summary>
		/// Returns true if the given rayOrigin intersects with the OverShoulderTrigger behind the player head model
		/// </summary>
		Func<Transform, bool> isOverShoulder { set; }
	}
}