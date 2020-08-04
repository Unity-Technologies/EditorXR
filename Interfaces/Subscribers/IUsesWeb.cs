using System;
using Unity.XRTools.ModuleLoader;
using UnityEngine.Networking;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to web requests
    /// </summary>
    public interface IUsesWeb : IFunctionalitySubscriber<IProvidesWeb>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesConnectInterfaces
    /// </summary>
    public static class UsesWebMethods
    {
        /// <summary>
        /// Download a resource at the given URL and call a method on completion, providing the UnityWebRequest
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="url">The URL of the resource</param>
        /// <param name="completed">The completion callback</param>
        public static void Download(this IUsesWeb user, string url, Action<UnityWebRequest> completed)
        {
#if !FI_AUTOFILL
            user.provider.Download(url, completed);
#endif
        }

        /// <summary>
        /// Download a resource at the given URL using a custom download handler and call a method on completion, providing the UnityWebRequest
        /// </summary>
        /// <typeparam name="THandler">The type of download handler to use</typeparam>
        /// <param name="user">The functionality user</param>
        /// <param name="url">The URL of the resource</param>
        /// <param name="completed">The completion callback</param>
        public static void Download<THandler>(this IUsesWeb user, string url, Action<UnityWebRequest> completed) where THandler : DownloadHandler, new()
        {
#if !FI_AUTOFILL
            user.provider.Download<THandler>(url, completed);
#endif
        }

        /// <summary>
        /// Download a resource at the given URL to the given destination file and call a method on completion
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="url">The URL of the resource</param>
        /// <param name="destination">The destination file path</param>
        /// <param name="completed">The completion callback</param>
        public static void Download(this IUsesWeb user, string url, string destination, Action completed)
        {
#if !FI_AUTOFILL
            user.provider.Download(url, destination, completed);
#endif
        }
    }
}
