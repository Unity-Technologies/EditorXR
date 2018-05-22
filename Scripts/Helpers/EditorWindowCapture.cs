#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
using System.Threading;

#endif

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    /// <summary>
    /// Captures a RenderTexture representing an Editor window
    /// </summary>
    sealed class EditorWindowCapture : MonoBehaviour
    {
        // Offset for window header (internally defined in Unity) when sending events
        // Mouse events are expected to be relative to the window, but our quad only displays the inner GUI
        static readonly Vector2 k_WindowOffset = new Vector2(0, 22f);

        [SerializeField]
        string m_WindowClass;

        [SerializeField]
        Rect m_Position = new Rect(0f, 0f, 600f, 400f);

        EditorWindow m_Window;
        Object m_GuiView;
        MethodInfo m_GrabPixels;

        /// <summary>
        /// RenderTexture that represents the captured Editor Window
        /// Updated frequently, when capture is enabled
        /// </summary>
        public RenderTexture texture { get; private set; }

        public bool capture { get; set; }

        // Local method use only -- created here to reduce garbage collection
        static readonly object[] k_GrabPixelsArgs = new object[2];

        void Start()
        {
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

                var parentField = windowType.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
                m_GuiView = (Object)parentField.GetValue(m_Window);

                // It's necessary to force a repaint on first start-up of window
                var repaint = windowType.GetMethod("RepaintImmediately", BindingFlags.Instance | BindingFlags.NonPublic);
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
                var rect = m_Position;

                // GrabPixels is relative to the GUIView and not the desktop, so we don't care about the offset
                rect.x = 0f;
                rect.y = 0f;
                var width = Mathf.RoundToInt(rect.width);
                var height = Mathf.RoundToInt(rect.height);

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


                k_GrabPixelsArgs[0] = texture;
                k_GrabPixelsArgs[1] = rect;
                m_GrabPixels.Invoke(m_GuiView, k_GrabPixelsArgs);
            }
        }

        public void SendEvent(Transform rayOrigin, Transform workspace, EventType type)
        {
            if (m_Window == null)
                return;

            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            var plane = new Plane(workspace.up, workspace.position);
            float distance;
            plane.Raycast(ray, out distance);
            var localPosition = transform.parent.InverseTransformPoint(ray.GetPoint(distance));
            localPosition.x += 0.5f;
            localPosition.y = -localPosition.z + 0.5f;

            var rect = m_Window.position;
            var clickPosition = Vector2.Scale(localPosition, rect.size);

            if (clickPosition.y < 0) // Click y positions below 0 move the window and cause issues
                return;

            clickPosition += k_WindowOffset;

#if UNITY_EDITOR_WIN

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
#endif

            m_Window.SendEvent(new Event
            {
                type = type,
                mousePosition = clickPosition
            });
        }

#if UNITY_EDITOR_WIN
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, IntPtr lParam);
#endif
    }
}
#endif