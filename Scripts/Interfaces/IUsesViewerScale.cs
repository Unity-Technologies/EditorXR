#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IUsesViewerScale
	{
		Func<float> getViewerScale { set; }
	}
}
#endif
