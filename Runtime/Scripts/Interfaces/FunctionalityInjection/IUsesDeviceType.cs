using System;
using UnityEngine.XR;

namespace Unity.Labs.EditorXR
{
    enum DeviceType
    {
        Oculus,
        Vive
    }

    /// <summary>
    /// In cases where you must have different input logic (e.g. button press + axis input) you can get the device type
    /// </summary>
    interface IUsesDeviceType
    {
    }

    static class UsesDeviceTypeMethods
    {
        static string s_XRDeviceModel;

        /// <summary>
        /// Returns the type of device currently in use
        /// </summary>
        /// <returns>The device type</returns>
        public static DeviceType GetDeviceType(this IUsesDeviceType @this)
        {
            if (string.IsNullOrEmpty(s_XRDeviceModel))
                s_XRDeviceModel = XRDevice.model;

            return s_XRDeviceModel.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0
                ? DeviceType.Oculus : DeviceType.Vive;
        }
    }
}
