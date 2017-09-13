using System;
using UnityEngine.Networking;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides access to the Web Module
	/// </summary>
	public interface IWeb { }

	public static class IWebMethods
	{
		internal static Action<string, Action<DownloadHandler>> download;
		internal static Action<string, string, Action> downloadToDisk;

		public static void Download(this IWeb obj, string url, Action<DownloadHandler> completed)
		{
			download(url, completed);
		}

		public static void Download(this IWeb obj, string url, string destination, Action completed)
		{
			downloadToDisk(url, destination, completed);
		}
	}
}
