#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	[MainMenuItem("Console", "Workspaces", "View errors, warnings and other messages")]
	sealed class ConsoleWorkspace : Workspace
	{
		public static Vector2 mousePosition;

		[SerializeField]
		GameObject m_ConsoleWindowPrefab;

		Transform m_ConsoleWindow;

		EditorWindow m_Console;

		RectTransform m_ConsoleRect;

		public override void Setup()
		{
			// Initial bounds must be set before the base.Setup() is called
			minBounds = new Vector3(0.6f, MinBounds.y, 0.5f);
			m_CustomStartingBounds = minBounds;

			base.Setup();

			preventFrontBackResize = true;
			preventLeftRightResize = true;
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
			handle.dragEnded += OnDragEnded;
			m_ConsoleRect = m_ConsoleWindow.GetComponent<RectTransform>();

			EditorApplication.delayCall += () =>
			{
				m_Console = m_ConsoleWindow.GetComponent<EditorWindowCapture>().window;
			};
		}

		void SendEvent(HandleEventData eventData, EventType type)
		{
			if (m_Console == null)
				return;

			var rayOrigin = eventData.rayOrigin;
			var ray = new Ray(rayOrigin.position, rayOrigin.forward);
			var plane = new Plane(transform.up, transform.position);
			float distance;
			plane.Raycast(ray, out distance);
			var localPosition = transform.InverseTransformPoint(ray.GetPoint(distance));
			var worldCorners = new Vector3[4];
			m_ConsoleRect.GetWorldCorners(worldCorners);
			var contentSize = new Vector2((worldCorners[1] - worldCorners[2]).magnitude,
				(worldCorners[0] - worldCorners[1]).magnitude);
			localPosition.x /= contentSize.x;
			localPosition.z /= -contentSize.y;
			var rectPosition = new Vector2(localPosition.x + 0.5f, localPosition.z + 0.5f);

			var rect = m_Console.position;
			var clickPosition = Vector2.Scale(rectPosition, rect.size);

			mousePosition = clickPosition;
			m_Console.Repaint();

			m_Console.SendEvent(new Event
			{
				type = type,
				mousePosition = clickPosition
			});
		}

		void OnHovering(BaseHandle handle, HandleEventData eventData)
		{
			SendEvent(eventData, EventType.MouseMove);
		}

		void OnDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			SendEvent(eventData, EventType.mouseDown);
		}

		void OnDragEnded(BaseHandle handle, HandleEventData eventData)
		{
			SendEvent(eventData, EventType.mouseUp);
		}
	}
}
#endif
