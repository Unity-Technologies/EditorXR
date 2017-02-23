#if UNITY_EDITOR
using ListView;
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	class DraggableListItem<DataType> : ListViewItem<DataType>, IGetPreviewOrigin where DataType : ListViewItemData
	{
		const float k_MagnetizeDuration = 0.5f;

		protected Transform m_DragObject;

		protected float m_DragLerp;

		public Func<Transform, Transform> getPreviewOriginForRayOrigin { set; protected get; }

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
			while (currTime < k_MagnetizeDuration)
			{
				currTime = Time.realtimeSinceStartup - startTime;
				m_DragLerp = currTime / k_MagnetizeDuration;
				yield return null;
			}
			m_DragLerp = 1;
		}

		protected virtual void OnDragging(BaseHandle baseHandle, HandleEventData eventData)
		{
			if (m_DragObject)
			{
				var previewOrigin = getPreviewOriginForRayOrigin(eventData.rayOrigin);
				MathUtilsExt.LerpTransform(m_DragObject, previewOrigin.position, previewOrigin.rotation, m_DragLerp);
			}
		}

		protected virtual void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
		{
			m_DragObject = null;
		}
	}
}
#endif
