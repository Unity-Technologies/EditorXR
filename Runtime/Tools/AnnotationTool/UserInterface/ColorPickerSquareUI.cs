using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.EditorXR.Tools
{
    class ColorPickerSquareUI : Selectable, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        bool m_AllowDragEvents;

        public Action onDrag { private get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_AllowDragEvents = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (onDrag != null && m_AllowDragEvents)
                onDrag();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_AllowDragEvents = false;
        }
    }
}
