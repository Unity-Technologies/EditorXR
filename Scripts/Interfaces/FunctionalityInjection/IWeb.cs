#if UNITY_EDITOR
using System;
using UnityEngine.Networking;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the Web Module
    /// </summary>
    public interface IWeb
    {
    }

    public static class IWebMethods
    {
        // TODO: Template support for Functionality Injection
        internal static Action<string, Action<DownloadHandler>> download;
        internal static Action<string, Action<DownloadHandlerTexture>> downloadTexture;
        internal static Action<string, string, Action> downloadToDisk;

        public static void Download(this IWeb obj, string url, Action<DownloadHandler> completed)
        {
            download(url, completed);
        }

        public static void DownloadTexture(this IWeb obj, string url, Action<DownloadHandlerTexture> completed)
        {
            downloadTexture(url, completed);
        }

        public static void Download(this IWeb obj, string url, string destination, Action completed)
        {
            downloadToDisk(url, destination, completed);
        }
    }
}
#endif
