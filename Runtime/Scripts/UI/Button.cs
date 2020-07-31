using Unity.EditorXR.Modules;
using Unity.EditorXR.Utilities;
using Unity.XRTools.Utils.GUI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Unity.EditorXR.UI
{
    /// <summary>
    /// Extension of UI.Button includes SelectionFlags to check for direct selection
    /// </summary>
    sealed class Button : UnityEngine.UI.Button, ISelectionFlags
    {
        public SelectionFlags selectionFlags
        {
            get { return m_SelectionFlags; }
            set { m_SelectionFlags = value; }
        }

        [SerializeField]
        [FlagsProperty]
        SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

        public UnityEvent onEnter = new UnityEvent();
        public UnityEvent onExit = new UnityEvent();
        public UnityEvent onDown = new UnityEvent();
        public UnityEvent onUp = new UnityEvent();

        public override void OnPointerClick(PointerEventData eventData)
        {
            var rayEventData = eventData as RayEventData;
            if (rayEventData == null || UIUtils.IsValidEvent(rayEventData, selectionFlags))
                base.OnPointerClick(eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            var rayEventData = eventData as RayEventData;
            if (rayEventData == null || UIUtils.IsValidEvent(rayEventData, selectionFlags))
            {
                base.OnPointerEnter(eventData);
                onEnter.Invoke();
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            onExit.Invoke();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            var rayEventData = eventData as RayEventData;
            if (rayEventData == null || UIUtils.IsValidEvent(rayEventData, selectionFlags))
            {
                base.OnPointerDown(eventData);
                onDown.Invoke();
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            var rayEventData = eventData as RayEventData;
            if (rayEventData == null || UIUtils.IsValidEvent(rayEventData, selectionFlags))
            {
                base.OnPointerUp(eventData);
                onUp.Invoke();
            }
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            var rayEventData = eventData as RayEventData;
            if (rayEventData == null || UIUtils.IsValidEvent(rayEventData, selectionFlags))
                base.OnSubmit(eventData);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            //Not selectable
        }
    }
}
