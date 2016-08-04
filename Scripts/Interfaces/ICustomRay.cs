using System;

namespace UnityEngine.VR.Tools
{
	public interface ICustomRay : IRay
	{
		/// <summary>
		/// Method handling the enabling & showing of the default proxy ray
		/// </summary>
		Action showDefaultRay { set; }

		/// <summary>
		/// Method handling the disabling & hiding of the default proxy ray
		/// </summary>
		Action hideDefaultRay { set; }

		//TODO: Handle the disabling of the RayOrigin preventing further raycasting
	}
}