using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    static class ExecuteRayEvents
    {
        public static ExecuteEvents.EventFunction<IRayPointerDownHandler> pointerDownHandler { get { return k_PointerDownHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayPointerDownHandler> k_PointerDownHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayBeginDragHandler> beginDragHandler { get { return k_BeginDragHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayBeginDragHandler> k_BeginDragHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayDragHandler> dragHandler { get { return k_DragHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayDragHandler> k_DragHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayEndDragHandler> endDragHandler { get { return k_EndDragHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayEndDragHandler> k_EndDragHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayPointerUpHandler> pointerUpHandler { get { return k_PointerUpHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayPointerUpHandler> k_PointerUpHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayEnterHandler> rayEnterHandler { get { return k_RayEnterHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayEnterHandler> k_RayEnterHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayExitHandler> rayExitHandler { get { return k_RayExitHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayExitHandler> k_RayExitHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayHoverHandler> rayHoverHandler { get { return k_RayHoverHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayHoverHandler> k_RayHoverHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayClickHandler> pointerClickHandler { get { return k_PointerClickHandler; } }
        static readonly ExecuteEvents.EventFunction<IRayClickHandler> k_PointerClickHandler = Execute;

        static void Execute(IRayPointerDownHandler handler, BaseEventData eventData)
        {
            handler.OnPointerDown(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayBeginDragHandler handler, BaseEventData eventData)
        {
            handler.OnBeginDrag(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayDragHandler handler, BaseEventData eventData)
        {
            handler.OnDrag(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayEndDragHandler handler, BaseEventData eventData)
        {
            handler.OnEndDrag(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayPointerUpHandler handler, BaseEventData eventData)
        {
            handler.OnPointerUp(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayEnterHandler handler, BaseEventData eventData)
        {
            handler.OnRayEnter(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayExitHandler handler, BaseEventData eventData)
        {
            handler.OnRayExit(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayHoverHandler handler, BaseEventData eventData)
        {
            handler.OnRayHover(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        static void Execute(IRayClickHandler handler, BaseEventData eventData)
        {
            handler.OnRayClick(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }
    }
}
