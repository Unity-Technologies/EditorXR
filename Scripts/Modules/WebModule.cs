#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class WebModule : MonoBehaviour
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

        void Update()
        {
            var completedRequests = new List<string>();
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

                    completedRequests.Add(kvp.Key);
                }
            }

            foreach (var request in completedRequests)
            {
                m_Requests.Remove(request);
            }

            while (m_Requests.Count < k_MaxSimultaneousRequests && m_QueuedRequests.Count > 0)
            {
                var first = m_QueuedRequests.Dequeue();
                m_Requests.Add(first.key, first);
            }

            //TODO: Generalize Request class
            completedRequests.Clear();
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

                    completedRequests.Add(kvp.Key);
                }
            }

            foreach (var request in completedRequests)
            {
                m_TextureRequests.Remove(request);
            }

            while (m_TextureRequests.Count < k_MaxSimultaneousRequests && m_QueuedTextureRequests.Count > 0)
            {
                var first = m_QueuedTextureRequests.Dequeue();
                m_TextureRequests.Add(first.key, first);
            }

            var completedTransfers = new List<FileTransfer>();
            foreach (var transfer in m_Transfers)
            {
                if (transfer.isDone)
                {
                    transfer.Complete();
                    completedTransfers.Add(transfer);
                }
            }

            foreach (var transfer in completedTransfers)
            {
                m_Transfers.Remove(transfer);
            }

            while (m_Transfers.Count < k_MaxSimultaneousTransfers && m_QueuedTransfers.Count > 0)
            {
                var first = m_QueuedTransfers.Dequeue();
                m_Transfers.Add(first);
            }
        }
    }
}
#endif
