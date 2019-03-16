#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.XR;

namespace UnityEditor.Experimental.EditorVR.Input
{
    sealed class OVRTouchInputToEvents : BaseVRInputToEvents
    {
        protected override string DeviceName
        {
            get { return "Oculus Touch Controller"; }
        }
    }
}
#endif