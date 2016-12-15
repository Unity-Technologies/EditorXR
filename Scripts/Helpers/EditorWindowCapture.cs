#if !UNITY_EDITOR
#pragma warning disable 414
#endif

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Reflection;
#endif
using Object = UnityEngine.Object;

/// <summary>
/// Captures a RenderTexture representing an Editor window
/// </summary>
public class EditorWindowCapture : MonoBehaviour
{
	[SerializeField]
	private string m_WindowClass = "UnityEditor.ProfilerWindow";
	[SerializeField]
	private Rect m_Position = new Rect(0f, 0f, 600f, 400f);

#if UNITY_EDITOR
	private EditorWindow m_Window;
	private Object m_GuiView;
	private MethodInfo m_GrabPixels;

	/// <summary>
	/// RenderTexture that represents the captured Editor Window
	/// Updated frequently, when capture is enabled
	/// </summary>
	public RenderTexture texture { get; private set; }
	public bool capture { get; set; }

	private void Start()
	{
		Assembly asm = Assembly.GetAssembly(typeof(UnityEditor.Editor));
		Type windowType = asm.GetType(m_WindowClass);
		Type guiViewType = asm.GetType("UnityEditor.GUIView");
		if (windowType != null && guiViewType != null)
		{
			m_Window = EditorWindow.CreateInstance(windowType) as EditorWindow;
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

	private void OnDisable()
	{
		if (m_Window)
			m_Window.Close();
	}

	private void Update()
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
#endif
}
