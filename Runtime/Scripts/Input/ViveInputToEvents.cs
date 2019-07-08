using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Input
{
    sealed class ViveInputToEvents : BaseVRInputToEvents
    {
        protected override string DeviceName
        {
            get { return "OpenVR Controller"; }
        }

        protected override string GetButtonAxis(VRInputDevice.Handedness hand, VRInputDevice.VRControl button)
        {
            // For some reason primary/secondary are swapped in OpenVR
            switch (button)
            {
                case VRInputDevice.VRControl.Action1:
                    if (hand == VRInputDevice.Handedness.Left)
                        return "XRI_Left_SecondaryButton";
                    else
                        return "XRI_Right_SecondaryButton";

                case VRInputDevice.VRControl.Action2:
                    if (hand == VRInputDevice.Handedness.Left)
                        return "XRI_Left_PrimaryButton";
                    else
                        return "XRI_Right_PrimaryButton";

                case VRInputDevice.VRControl.LeftStickButton:
                    if (hand == VRInputDevice.Handedness.Left)
                        return "XRI_Left_Primary2DAxisClick";
                    else
                        return "XRI_Right_Primary2DAxisClick";
            }

            // Not all buttons are currently mapped
            return null;
        }
    }
}
