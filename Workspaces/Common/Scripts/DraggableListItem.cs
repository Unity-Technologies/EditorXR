#if UNITY_EDITOR
using ListView;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	class DraggableListItem<DataType, IndexType> : ListViewItem<DataType, IndexType>, IGetPreviewOrigin, IUsesViewerScale
		where DataType : ListViewItemData<IndexType>
	{
		const float k_MagnetizeDuration = 0.5f;
		const float kDragDeadzone = 0.025f;
		const float kHorizThreshold = 0.9f;

		protected Transform m_DragObject;

		protected float m_DragLerp;

		bool m_HorizontalDrag;

		readonly Dictionary<Transform, Vector3> m_DragStarts = new Dictionary<Transform, Vector3>();

		protected virtual bool singleClickDrag { get { return true; } }
		//protected virtual BaseHandle clickedHandle { get; set; }

		public Func<Transform, Transform> getPreviewOriginForRayOrigin { set; protected get; }

		public Func<float> getViewerScale { get; set; }

		protected virtual void OnDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			if (singleClickDrag)
			{
				m_DragObject = handle.transform;
				m_DragLerp = 0;
				StartCoroutine(Magnetize());
			}
			else
			{
				m_DragObject = null;
				m_DragStarts[eventData.rayOrigin] = eventData.rayOrigin.position;
			}
		}

		// Smoothly interpolate grabbed object into position, instead of "popping."
		protected virtual IEnumerator Magnetize()
		{
			var startTime = Time.realtimeSinceStartup;
			var currTime = 0f;
			while (currTime < k_MagnetizeDuration)
			{
				currTime = Time.realtimeSinceStartup - startTime;
				m_DragLerp = currTime / k_MagnetizeDuration;
				yield return null;
			}
			m_DragLerp = 1;
			OnMagnetizeEnded();
		}

		protected virtual void OnMagnetizeEnded()
		{
		}

		protected virtual void OnDragging(BaseHandle handle, HandleEventData eventData)
		{
			if (singleClickDrag)
			{
				if (m_DragObject)
				{
					var previewOrigin = getPreviewOriginForRayOrigin(eventData.rayOrigin);
					MathUtilsExt.LerpTransform(m_DragObject, previewOrigin.position, previewOrigin.rotation, m_DragLerp);
				}
			}
			else
			{
				var rayOrigin = eventData.rayOrigin;
				var dragStart = m_DragStarts[rayOrigin];
				var dragVector = rayOrigin.position - dragStart;
				var distance = dragVector.magnitude;

				if (m_DragObject == null && distance > kDragDeadzone * getViewerScale())
				{
					m_DragObject = handle.transform;
					m_HorizontalDrag = Mathf.Abs(Vector3.Dot(dragVector, m_DragObject.right)) / distance > kHorizThreshold;

					if (m_HorizontalDrag)
						OnHorizontalDragStart(handle, eventData);
					else
						OnVerticalDragStart(handle, eventData);
				}

				if (m_DragObject)
				{
					if (m_HorizontalDrag)
						OnHorizontalDrag(handle, eventData, dragStart);
					else
						OnVerticalDrag(handle, eventData, dragStart);
				}
			}
		}

		protected virtual void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
		{
			m_DragObject = null;
		}

		//protected virtual void OnSingleClick(BaseHandle handle, HandleEventData eventData)
		//{
		//}

		//protected virtual void OnDoubleClick(BaseHandle handle, HandleEventData eventData)
		//{
		//}

		protected virtual void OnHorizontalDragStart(BaseHandle handle, HandleEventData eventData)
		{
		}

		protected virtual void OnVerticalDragStart(BaseHandle handle, HandleEventData eventData)
		{
		}

		protected virtual void OnHorizontalDrag(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
		{
		}

		protected virtual void OnVerticalDrag(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
		{
		}
	}
}
#endif