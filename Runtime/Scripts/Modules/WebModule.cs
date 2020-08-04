using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.EditorXR.Interfaces;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.EditorXR.Modules
{
    class WebModule : IModuleBehaviorCallbacks, IProvidesWeb
    {
        class DownloadRequest
        {
            public string key; // For queuing
            public UnityWebRequest request;
            public Action<UnityWebRequest> completed;

            public void Complete()
            {
                if (completed != null)
                    completed(request);
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

        readonly List<FileTransfer> m_Transfers = new List<FileTransfer>();
        readonly Queue<FileTransfer> m_QueuedTransfers = new Queue<FileTransfer>();

        List<string> m_CompletedRequests = new List<string>(20);
        List<FileTransfer> m_CompletedTransfers = new List<FileTransfer>(20);

        /// <summary>
        /// Download a resource at the given URL and call a method on completion, providing the UnityWebRequest
        /// </summary>
        /// <param name="url">The URL of the resource</param>
        /// <param name="completed">The completion callback</param>
        public void Download(string url, Action<UnityWebRequest> completed)
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

                request.completed += completed;
            }
        }

        /// <summary>
        /// Download a resource at the given URL using a custom download handler and call a method on completion, providing the UnityWebRequest
        /// </summary>
        /// <typeparam name="THandler">The type of download handler to use</typeparam>
        /// <param name="url">The URL of the resource</param>
        /// <param name="completed">The completion callback</param>
        public void Download<THandler>(string url, Action<UnityWebRequest> completed) where THandler : DownloadHandler, new()
        {
            DownloadRequest request;
            if (!m_Requests.TryGetValue(url, out request))
            {
                var webRequest = UnityWebRequest.Get(url);
                webRequest.downloadHandler = new THandler();
                webRequest.SendWebRequest();
                request = new DownloadRequest { key = url, request = webRequest };
                if (m_Requests.Count < k_MaxSimultaneousRequests)
                    m_Requests.Add(url, request);
                else
                    m_QueuedRequests.Enqueue(request);

                request.completed += completed;
            }
        }

        /// <summary>
        /// Download a resource at the given URL to the given destination file and call a method on completion
        /// </summary>
        /// <param name="url">The URL of the resource</param>
        /// <param name="destination">The destination file path</param>
        /// <param name="completed">The completion callback</param>
        public void Download(string url, string destination, Action completed)
        {
            Download(url, request =>
            {
                var transfer = new FileTransfer();
                transfer.completed += completed;
                m_Transfers.Add(transfer);
                var data = request.downloadHandler.data;
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

            m_CompletedRequests.Clear();

            while (m_Requests.Count < k_MaxSimultaneousRequests && m_QueuedRequests.Count > 0)
            {
                var first = m_QueuedRequests.Dequeue();
                m_Requests.Add(first.key, first);
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

            m_CompletedTransfers.Clear();
        }

        public void LoadModule() { }

        public void UnloadModule() { }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var webSubscriber = obj as IFunctionalitySubscriber<IProvidesWeb>;
            if (webSubscriber != null)
                webSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
