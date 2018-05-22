
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    static class ExecuteRayEvents
    {
        public static ExecuteEvents.EventFunction<IRayBeginDragHandler> beginDragHandler { get { return s_BeginDragHandler; } }
        private static readonly ExecuteEvents.EventFunction<IRayBeginDragHandler> s_BeginDragHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayDragHandler> dragHandler { get { return s_DragHandler; } }
        private static readonly ExecuteEvents.EventFunction<IRayDragHandler> s_DragHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayEndDragHandler> endDragHandler { get { return s_EndDragHandler; } }
        private static readonly ExecuteEvents.EventFunction<IRayEndDragHandler> s_EndDragHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayEnterHandler> rayEnterHandler { get { return s_RayEnterHandler; } }
        private static readonly ExecuteEvents.EventFunction<IRayEnterHandler> s_RayEnterHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayExitHandler> rayExitHandler { get { return s_RayExitHandler; } }
        private static readonly ExecuteEvents.EventFunction<IRayExitHandler> s_RayExitHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayHoverHandler> rayHoverHandler { get { return s_RayHoverHandler; } }
        private static readonly ExecuteEvents.EventFunction<IRayHoverHandler> s_RayHoverHandler = Execute;

        public static ExecuteEvents.EventFunction<IRayClickHandler> rayClickHandler { get { return s_RayClickHandler; } }
        private static readonly ExecuteEvents.EventFunction<IRayClickHandler> s_RayClickHandler = Execute;

        private static void Execute(IRayBeginDragHandler handler, BaseEventData eventData)
        {
            handler.OnBeginDrag(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        private static void Execute(IRayDragHandler handler, BaseEventData eventData)
        {
            handler.OnDrag(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        private static void Execute(IRayEndDragHandler handler, BaseEventData eventData)
        {
            handler.OnEndDrag(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        private static void Execute(IRayEnterHandler handler, BaseEventData eventData)
        {
            handler.OnRayEnter(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        private static void Execute(IRayExitHandler handler, BaseEventData eventData)
        {
            handler.OnRayExit(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        private static void Execute(IRayHoverHandler handler, BaseEventData eventData)
        {
            handler.OnRayHover(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }

        private static void Execute(IRayClickHandler handler, BaseEventData eventData)
        {
            handler.OnRayClick(ExecuteEvents.ValidateEventData<RayEventData>(eventData));
        }
    }
}

