using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public class StandardManipulator : MonoBehaviour, IManipulator
{
	[SerializeField]
	private Transform m_PlaneHandlesParent;

	[SerializeField]
	private List<BaseHandle> m_AllHandles;

	public bool dragging { get {  return m_Dragging; } }
	private bool m_Dragging;

	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }
	public bool active { get {return gameObject.activeSelf; } set { gameObject.SetActive(value); } }
	public new Transform transform { get { return transform; } }

	void OnEnable()
	{
		foreach (var h in m_AllHandles)
		{
			if (h is LinearHandle || h is PlaneHandle || h is SphereHandle)
				h.dragging += OnTranslateDragging;

			if (h is RadialHandle)
				h.dragging += OnRotateDragging;

			h.dragStarted += OnHandleDragStarted;
			h.dragEnded += OnHandleDragEnded;
		}
	}

	void OnDisable()
	{
		foreach (var h in m_AllHandles)
		{
			if (h is LinearHandle || h is PlaneHandle || h is SphereHandle)
				h.dragging -= OnTranslateDragging;

			if (h is RadialHandle)
				h.dragging -= OnRotateDragging;

			h.dragStarted -= OnHandleDragStarted;
			h.dragEnded -= OnHandleDragEnded;
		}
	}

	void Update()
	{
		if (!m_Dragging)
		{
			// Place the plane handles in a good location that is accessible to the user
			var viewerPosition = U.Camera.GetMainCamera().transform.position;
			foreach (Transform t in m_PlaneHandlesParent)
			{
				var localPos = t.localPosition;
				localPos.x = Mathf.Abs(localPos.x) * (transform.position.x < viewerPosition.x ? 1 : -1);
				localPos.y = Mathf.Abs(localPos.y) * (transform.position.y < viewerPosition.y ? 1 : -1);
				localPos.z = Mathf.Abs(localPos.z) * (transform.position.z < viewerPosition.z ? 1 : -1);
				t.localPosition = localPos;
			}
		}
	}

	private void OnTranslateDragging(BaseHandle handle, HandleEventData eventData)
	{
		translate(eventData.deltaPosition);
	}

	private void OnRotateDragging(BaseHandle handle, HandleEventData eventData)
	{
		rotate(eventData.deltaRotation);
	}

	private void OnHandleDragStarted(BaseHandle handle, HandleEventData eventData)
	{
		foreach (var h in m_AllHandles)
			h.gameObject.SetActive(h == handle);

		m_Dragging = true;
	}

	private void OnHandleDragEnded(BaseHandle handle, HandleEventData eventData)
	{
		if (gameObject.activeSelf)
			foreach (var h in m_AllHandles)
				h.gameObject.SetActive(true);

		m_Dragging = false;
	}
}
