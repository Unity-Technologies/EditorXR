#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IUsesViewerScale
	{
	}

	public static class IUsesViewerScaleMethods
	{
		internal static Func<float> getViewerScale { get; set; }

		/// <summary>
		/// Returns whether the specified transform is over the viewer's shoulders and behind the head
		/// </summary>
		public static float GetViewerScale(this IUsesViewerScale obj)
		{
			if (getViewerScale != null)
				return getViewerScale();

			return 1;
		}
	}
}
#endif
