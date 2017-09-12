using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class WebModule : MonoBehaviour
{
	class Request
	{
		public string key; // For queuing
		public IEnumerator<DownloadHandler> enumerator;
		public readonly List<Action<DownloadHandler>> completed = new List<Action<DownloadHandler>>(1);
	}

	const int k_MaxSimultaneousRequests = 8;
	readonly Dictionary<string, Request> m_Requests = new Dictionary<string, Request>();
	readonly Queue<Request> m_QueuedRequests = new Queue<Request>();

	public void Download(string url, Action<DownloadHandler> completed)
	{
		Request request;
		if (!m_Requests.TryGetValue(url, out request))
		{
			request = new Request{ key = url, enumerator = DoDownload(url) };
			if (m_Requests.Count < k_MaxSimultaneousRequests)
				m_Requests.Add(url, request);
			else
				m_QueuedRequests.Enqueue(request);
		}

		request.completed.Add(completed);
	}

	static IEnumerator<DownloadHandler> DoDownload(string url)
	{
		using (var request = UnityWebRequest.Get(url))
		{
			request.Send();
			while (!request.isDone && !request.downloadHandler.isDone)
				yield return null;

			var error = request.error;
			if (!string.IsNullOrEmpty(error))
				Debug.LogWarning(error);

			yield return request.downloadHandler;
		}
	}

	void Update()
	{
		var completedRequests = new List<string>();
		foreach (var kvp in m_Requests)
		{
			var request = kvp.Value;
			var enumerator = request.enumerator;
			if (!enumerator.MoveNext())
			{
				var handler = enumerator.Current;
				if (handler != null)
				{
					foreach (var completed in request.completed)
					{
						completed.Invoke(handler);
					}
				}

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
	}
}
