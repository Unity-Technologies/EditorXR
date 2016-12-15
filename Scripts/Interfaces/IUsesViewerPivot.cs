namespace UnityEngine.Experimental.EditorVR.Tools
{
    /// <summary>
    /// Gives decorated class access to the Viewer Pivot
    /// </summary>
    public interface IUsesViewerPivot
	{
	    /// <summary>
	    /// The Viewer Pivot
	    /// </summary>
		Transform viewerPivot { set; }
	}
}