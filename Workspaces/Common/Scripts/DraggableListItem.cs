using ListView;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;

public class DraggableListItem<DataType> : ListViewItem<DataType>, IPreview where DataType : ListViewItemData
{
	const float kMagnetizeDuration = 0.5f;

	protected Transform m_DragObject;

	protected float m_DragLerp;

	public Func<Transform, Transform> getPreviewOriginForRayOrigin { set; protected get; }

	public PreviewDelegate preview { set; protected get; }

	protected virtual void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_DragObject = baseHandle.transform;
		m_DragLerp = 0;
		StartCoroutine(Magnetize());
	}

	// Smoothly interpolate grabbed object into position, instead of "popping."
	IEnumerator Magnetize()
	{
		var startTime = Time.realtimeSinceStartup;
		var currTime = 0f;
		while (currTime < kMagnetizeDuration)
		{
			currTime = Time.realtimeSinceStartup - startTime;
			m_DragLerp = currTime / kMagnetizeDuration;
			yield return null;
		}
		m_DragLerp = 1;
	}

	protected virtual void OnDragging(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (m_DragObject)
			preview(m_DragObject, getPreviewOriginForRayOrigin(eventData.rayOrigin), m_DragLerp);
	}

	protected virtual void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_DragObject = null;
	}
}