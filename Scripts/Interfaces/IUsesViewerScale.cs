using System;

namespace UnityEngine.Experimental.EditorVR.Tools
{
	public interface IUsesViewerScale
	{
		Func<float> getViewerScale { set; }
	}
}
