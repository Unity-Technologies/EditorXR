#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	[MainMenuItem("Console", "Workspaces", "View errors, warnings and other messages")]
	sealed class ConsoleWorkspace : Workspace
	{
		static readonly Vector2 k_PointerOffset = new Vector2(0, 20f);

		[SerializeField]
		GameObject m_ConsoleWindowPrefab;

		Transform m_ConsoleWindow;

		EditorWindowCapture m_Capture;

		public override void Setup()
		{
			// Initial bounds must be set before the base.Setup() is called
			minBounds = new Vector3(0.6f, MinBounds.y, 0.4f);
			m_CustomStartingBounds = minBounds;

			base.Setup();

			preventResize = true;
			dynamicFaceAdjustment = false;

			m_ConsoleWindow = this.InstantiateUI(m_ConsoleWindowPrefab).transform;
			m_ConsoleWindow.SetParent(m_WorkspaceUI.topFaceContainer, false);
			m_ConsoleWindow.localPosition = new Vector3(0f, -0.007f, -0.5f);
			m_ConsoleWindow.localRotation = Quaternion.Euler(90f, 0f, 0f);
			m_ConsoleWindow.localScale = new Vector3(1f, 1f, 1f);

			var bounds = contentBounds;
			var size = bounds.size;
			size.z = 0.1f;
			bounds.size = size;
			contentBounds = bounds;

			var handle = m_ConsoleWindow.GetComponent<BaseHandle>();
			handle.hovering += OnHovering;
			handle.dragStarted += OnDragStarted;
			handle.dragging += OnDragging;
			handle.dragEnded += OnDragEnded;

			m_Capture = m_ConsoleWindow.GetComponent<EditorWindowCapture>();
		}

		void OnHovering(BaseHandle handle, HandleEventData eventData)
		{
			m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseMove, k_PointerOffset);
		}

		void OnDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseDown, k_PointerOffset);
		}

		void OnDragging(BaseHandle handle, HandleEventData eventData)
		{
			m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseDrag, k_PointerOffset);
		}

		void OnDragEnded(BaseHandle handle, HandleEventData eventData)
		{
			m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseUp, k_PointerOffset);
		}
	}
}
#endif
