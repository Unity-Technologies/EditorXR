
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

        /// <summary>
        /// Download the given URL
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="completed">A method to be called on completion</param>
        public static void Download(this IWeb obj, string url, Action<DownloadHandler> completed)
        {
            download(url, completed);
        }

        /// <summary>
        /// Download the given URL using a DownloadTextureHandler
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="completed">A method to be called on completion</param>
        public static void DownloadTexture(this IWeb obj, string url, Action<DownloadHandlerTexture> completed)
        {
            downloadTexture(url, completed);
        }

        /// <summary>
        /// Download the given URL to a file on disk
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="destination">The file in which to store the results</param>
        /// <param name="completed">A method to be called on completion</param>
        public static void Download(this IWeb obj, string url, string destination, Action completed)
        {
            downloadToDisk(url, destination, completed);
        }
    }
}

