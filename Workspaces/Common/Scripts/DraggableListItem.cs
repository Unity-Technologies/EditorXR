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
	class DraggableListItem<DataType, IndexType> : ListViewItem<DataType, IndexType>, IGetPreviewOrigin where DataType : ListViewItemData<IndexType>
	{
		const float k_MagnetizeDuration = 0.5f;
		const float kDragDeadzone = 0.025f;

		protected Transform m_DragObject;

		protected float m_DragLerp;

		protected int m_ClickCount;
		bool m_SelectIsHeld;

		readonly Dictionary<Transform, Vector3> m_DragStarts = new Dictionary<Transform, Vector3>();

		float m_LastClickTime;
		float m_DragDistance;

		protected virtual bool singleClickDrag { get { return true; } }

		protected virtual BaseHandle clickedHandle { get; set; }

		public Func<Transform, Transform> getPreviewOriginForRayOrigin { set; protected get; }

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

				if (m_ClickCount == 0)
				{
					clickedHandle = handle;
					StartCoroutine(CheckSingleClick(handle, eventData));
				}

				m_ClickCount++;
				m_SelectIsHeld = true;

				m_DragStarts[eventData.rayOrigin] = eventData.rayOrigin.position;

				var timeSinceLastClick = Time.realtimeSinceStartup - m_LastClickTime;
				m_LastClickTime = Time.realtimeSinceStartup;
				if (m_ClickCount > 1 && UIUtils.IsDoubleClick(timeSinceLastClick))
				{
					CancelSingleClick();
					OnDoubleClick(handle, eventData);
				}
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
				m_DragDistance = (rayOrigin.position - dragStart).magnitude;

				if (clickedHandle)
				{
					if (m_DragDistance > kDragDeadzone)
						CancelSingleClick();
				}

				OnSingleClickDrag(handle, eventData, dragStart);
			}
		}

		protected virtual void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
		{
			m_SelectIsHeld = false;
			m_DragObject = null;
		}

		protected virtual void OnSingleClick(BaseHandle handle, HandleEventData eventData)
		{
		}

		protected virtual void OnDoubleClick(BaseHandle handle, HandleEventData eventData)
		{
		}

		protected virtual void OnSingleClickDrag(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
		{
		}

		void CancelSingleClick()
		{
			m_ClickCount = 0;
		}

		IEnumerator CheckSingleClick(BaseHandle handle, HandleEventData eventData)
		{
			var start = Time.realtimeSinceStartup;
			var currTime = 0f;
			while (m_SelectIsHeld || currTime < UIUtils.DoubleClickIntervalMax)
			{
				currTime = Time.realtimeSinceStartup - start;
				yield return null;
			}

			if (m_ClickCount == 1)
				OnSingleClick(handle, eventData);

			m_ClickCount = 0;
			clickedHandle = null;
		}
	}
}
#endif