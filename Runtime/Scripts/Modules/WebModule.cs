using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class WebModule : IModuleBehaviorCallbacks
    {
        class DownloadRequest
        {
            public string key; // For queuing
            public UnityWebRequest request;
            public event Action<DownloadHandler> completed;

            public void Complete()
            {
                var handler = request.downloadHandler;
                if (completed != null)
                    completed(handler);
            }
        }

        class TextureRequest
        {
            public string key; // For queuing
            public UnityWebRequest request;
            public event Action<DownloadHandlerTexture> completed;

            public void Complete()
            {
                var handler = (DownloadHandlerTexture)request.downloadHandler;
                if (completed != null)
                    completed(handler);
            }
        }

        class FileTransfer
        {
            public bool isDone;
            public event Action completed;

            public void Complete()
            {
                if (completed != null)
                    completed();
            }
        }

        // Assume all requests to the same url are the same. If this changes, use entire header as the key
        const int k_MaxSimultaneousRequests = 8;
        const int k_MaxSimultaneousTransfers = 8;
        readonly Dictionary<string, DownloadRequest> m_Requests = new Dictionary<string, DownloadRequest>();
        readonly Queue<DownloadRequest> m_QueuedRequests = new Queue<DownloadRequest>();
        readonly Dictionary<string, TextureRequest> m_TextureRequests = new Dictionary<string, TextureRequest>();
        readonly Queue<TextureRequest> m_QueuedTextureRequests = new Queue<TextureRequest>();

        readonly List<FileTransfer> m_Transfers = new List<FileTransfer>();
        readonly Queue<FileTransfer> m_QueuedTransfers = new Queue<FileTransfer>();

        List<string> m_CompletedRequests = new List<string>(20);
        List<FileTransfer> m_CompletedTransfers = new List<FileTransfer>(20);

        /// <summary>
        /// Download a resource at the given URL and call a method on completion, providing the DownloadHandler
        /// </summary>
        /// <param name="url">The URL of the resource</param>
        /// <param name="completed">The completion callback</param>
        public void Download(string url, Action<DownloadHandler> completed)
        {
            DownloadRequest request;
            if (!m_Requests.TryGetValue(url, out request))
            {
                var webRequest = UnityWebRequest.Get(url);
                webRequest.SendWebRequest();
                request = new DownloadRequest { key = url, request = webRequest };
                if (m_Requests.Count < k_MaxSimultaneousRequests)
                    m_Requests.Add(url, request);
                else
                    m_QueuedRequests.Enqueue(request);
            }

            request.completed += completed;
        }

        /// <summary>
        /// Download a texture at a given URL and call a method on completion, providing the DownloadTextureHandler
        /// </summary>
        /// <param name="url">The URL of the resource</param>
        /// <param name="completed">The completion callback</param>
        public void DownloadTexture(string url, Action<DownloadHandlerTexture> completed)
        {
            TextureRequest request;
            if (!m_TextureRequests.TryGetValue(url, out request))
            {
                var webRequest = UnityWebRequest.Get(url);
                webRequest.downloadHandler = new DownloadHandlerTexture();
                webRequest.SendWebRequest();
                request = new TextureRequest { key = url, request = webRequest };
                if (m_Requests.Count < k_MaxSimultaneousRequests)
                    m_TextureRequests.Add(url, request);
                else
                    m_QueuedTextureRequests.Enqueue(request);
            }

            request.completed += completed;
        }

        /// <summary>
        /// Download a resource at the given URL to the given destination file and call a method on completion
        /// </summary>
        /// <param name="url">The URL of the resource</param>
        /// <param name="destination">The destination file path</param>
        /// <param name="completed">The completion callback</param>
        public void Download(string url, string destination, Action completed)
        {
            Download(url, handler =>
            {
                var transfer = new FileTransfer();
                transfer.completed += completed;
                m_Transfers.Add(transfer);
                var data = handler.data;
                new Thread(() =>
                {
                    File.WriteAllBytes(destination, data);
                    transfer.isDone = true;
                }).Start();
            });
        }

        public void OnBehaviorUpdate()
        {
            foreach (var kvp in m_Requests)
            {
                var request = kvp.Value;
                var webRequest = request.request;

                if (webRequest.isDone && webRequest.downloadHandler.isDone)
                {
                    var error = webRequest.error;
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogWarning(error);

                    request.Complete();

                    m_CompletedRequests.Add(kvp.Key);
                }
            }

            foreach (var request in m_CompletedRequests)
            {
                m_Requests.Remove(request);
            }

            while (m_Requests.Count < k_MaxSimultaneousRequests && m_QueuedRequests.Count > 0)
            {
                var first = m_QueuedRequests.Dequeue();
                m_Requests.Add(first.key, first);
            }

            //TODO: Generalize Request class
            m_CompletedRequests.Clear();

            foreach (var kvp in m_TextureRequests)
            {
                var request = kvp.Value;
                var webRequest = request.request;

                if (webRequest.isDone && webRequest.downloadHandler.isDone)
                {
                    var error = webRequest.error;
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogWarning(error);

                    request.Complete();

                    m_CompletedRequests.Add(kvp.Key);
                }
            }

            foreach (var request in m_CompletedRequests)
            {
                m_TextureRequests.Remove(request);
            }

            while (m_TextureRequests.Count < k_MaxSimultaneousRequests && m_QueuedTextureRequests.Count > 0)
            {
                var first = m_QueuedTextureRequests.Dequeue();
                m_TextureRequests.Add(first.key, first);
            }

            foreach (var transfer in m_Transfers)
            {
                if (transfer.isDone)
                {
                    transfer.Complete();
                    m_CompletedTransfers.Add(transfer);
                }
            }

            foreach (var transfer in m_CompletedTransfers)
            {
                m_Transfers.Remove(transfer);
            }

            while (m_Transfers.Count < k_MaxSimultaneousTransfers && m_QueuedTransfers.Count > 0)
            {
                var first = m_QueuedTransfers.Dequeue();
                m_Transfers.Add(first);
            }

            m_CompletedRequests.Clear();
            m_CompletedTransfers.Clear();
        }

        public void LoadModule()
        {
            IWebMethods.download = Download;
            IWebMethods.downloadTexture = DownloadTexture;
            IWebMethods.downloadToDisk = Download;
        }

        public void UnloadModule() { }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }
    }
}
