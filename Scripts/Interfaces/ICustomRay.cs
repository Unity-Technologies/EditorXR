using System.Collections.Generic;

namespace UnityEngine.VR.Tools
{
    public interface ICustomRay : IRay
    {
        /// <summary>
        /// The default DefaultVrLineRenderers to show/hide
        /// </summary>
        List<VRLineRenderer> defaultProxyLineRenderers { get; set; }
        
        /// <summary>
        /// Method handling the enabling & showing of the default proxy ray
        /// </summary>
        void ShowDefaultProxyRays();

        /// <summary>
        /// Method handling the disabling & hiding of the default proxy ray
        /// </summary>
        void HideDefaultProxyRays();

        //TODO: Handle the disabling of the RayOrigin preventing further raycasting
    }
}