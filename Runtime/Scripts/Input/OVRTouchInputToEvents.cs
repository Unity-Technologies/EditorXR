namespace Unity.EditorXR.Input
{
    sealed class OVRTouchInputToEvents : BaseVRInputToEvents
    {
        protected override string DeviceName
        {
            get { return "Oculus Touch Controller"; }
        }
    }
}
