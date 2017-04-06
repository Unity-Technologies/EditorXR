#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
	/// <summary>
	/// Captures a RenderTexture representing an Editor window
	/// </summary>
	sealed class EditorWindowCapture : MonoBehaviour
	{
		[SerializeField]
		string m_WindowClass = "UnityEditor.ProfilerWindow";
		[SerializeField]
		Rect m_Position = new Rect(0f, 0f, 600f, 400f);

		RectTransform m_RectTransform;

#if UNITY_EDITOR
		EditorWindow m_Window;
		Object m_GuiView;
		MethodInfo m_GrabPixels;

		/// <summary>
		/// RenderTexture that represents the captured Editor Window
		/// Updated frequently, when capture is enabled
		/// </summary>
		public RenderTexture texture { get; private set; }

		public EditorWindow window {get { return m_Window; } }

		public bool capture { get; set; }

		void Start()
		{
			m_RectTransform = GetComponent<RectTransform>();

			Type windowType = null;
			Type guiViewType = null;

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(m_WindowClass);
				if (type != null)
					windowType = type;

				type = assembly.GetType("UnityEditor.GUIView");
				if (type != null)
					guiViewType = type;
			}

			if (windowType != null && guiViewType != null)
			{
				m_Window = ScriptableObject.CreateInstance(windowType) as EditorWindow;

				// AE: The first assignment is to undock the window if it was docked and the second is to position it off screen
				//window.position = rect;
				m_Window.Show();
				m_Window.position = m_Position;

				// NOTE: Uncomment To grab any and all GUIViews
				//foreach (UnityEngine.Object view in Resources.FindObjectsOfTypeAll(guiViewType))
				//{
				//    Debug.Log(view.name);             
				//}

				FieldInfo parentField = windowType.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
				m_GuiView = (Object)parentField.GetValue(m_Window);

				// It's necessary to force a repaint on first start-up of window
				MethodInfo repaint = windowType.GetMethod("RepaintImmediately", BindingFlags.Instance | BindingFlags.NonPublic);
				repaint.Invoke(m_Window, null);

				m_GrabPixels = guiViewType.GetMethod("GrabPixels", BindingFlags.Instance | BindingFlags.NonPublic);

				capture = true;
			}
			else
			{
				Debug.LogError("Could not load " + m_WindowClass);
			}
		}

		void OnDisable()
		{
			if (m_Window)
				m_Window.Close();
		}

		void Update()
		{
			if (m_Window && capture)
			{
				Rect rect = m_Position;

				// GrabPixels is relative to the GUIView and not the desktop, so we don't care about the offset
				rect.x = 0f;
				rect.y = 0f;
				int width = Mathf.RoundToInt(rect.width);
				int height = Mathf.RoundToInt(rect.height);

				if (texture && (texture.width != width || texture.height != height))
				{
					texture.Release();
					texture = null;
				}

				if (!texture)
				{
					texture = new RenderTexture(width, height, 0);
					texture.wrapMode = TextureWrapMode.Repeat;
				}

				m_GrabPixels.Invoke(m_GuiView, new object[] { texture, rect });
			}
		}

		public void SendEvent(Transform rayOrigin, Transform workspace, EventType type, Vector2 offset = default(Vector2))
		{
			if (m_Window == null)
				return;

			var ray = new Ray(rayOrigin.position, rayOrigin.forward);
			var plane = new Plane(workspace.up, workspace.position);
			float distance;
			plane.Raycast(ray, out distance);
			var localPosition = workspace.InverseTransformPoint(ray.GetPoint(distance));
			var worldCorners = new Vector3[4];
			m_RectTransform.GetWorldCorners(worldCorners);
			var contentSize = new Vector2((worldCorners[1] - worldCorners[2]).magnitude,
				(worldCorners[0] - worldCorners[1]).magnitude);
			localPosition.x /= contentSize.x;
			localPosition.z /= -contentSize.y;
			var rectPosition = new Vector2(localPosition.x + 0.5f, localPosition.z + 0.5f);

			var rect = m_Window.position;
			var clickPosition = Vector2.Scale(rectPosition, rect.size) + offset;

			if (clickPosition.y < 25f) // Click y positions below 25 move the window and cause issues
				return;

			// Send a message to cancel context menus in case the user clicked a drop-down
			// Thread is needed because context menu blocks main thread
			if (type == EventType.MouseDown)
			{
				new Thread(() =>
				{
					const int HWND_BROADCAST = 0xffff;
					const int WM_CANCELMODE = 0x001F;
					var hwnd = new IntPtr(HWND_BROADCAST);
					SendMessage(hwnd, WM_CANCELMODE, 0, IntPtr.Zero);
				}).Start();
			}

			m_Window.SendEvent(new Event
			{
				type = type,
				mousePosition = clickPosition
			});
		}

		[DllImport("User32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, IntPtr lParam);
#endif
	}
}
#endif
