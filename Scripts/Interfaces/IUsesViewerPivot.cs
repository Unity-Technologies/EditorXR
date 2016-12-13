namespace UnityEngine.VR.Tools
{
    /// <summary>
    /// Gives decorated class access to the VRView's Viewer Pivot
    /// </summary>
    public interface IUsesViewerPivot
	{
	    /// <summary>
	    /// The VRView's Viewer Pivot
	    /// </summary>
		Transform viewerPivot { set; }
	}
}