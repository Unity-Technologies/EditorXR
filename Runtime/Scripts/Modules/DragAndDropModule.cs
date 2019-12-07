using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.EditorXR.Modules
{
    sealed class DragAndDropModule : IDelayedInitializationModule, IUsesUIEvents
    {
        readonly Dictionary<Transform, IDroppable> m_Droppables = new Dictionary<Transform, IDroppable>();
        readonly Dictionary<Transform, IDropReceiver> m_DropReceivers = new Dictionary<Transform, IDropReceiver>();

        readonly Dictionary<Transform, GameObject> m_HoverObjects = new Dictionary<Transform, GameObject>();

        public int initializationOrder { get { return 1; } }

        public int shutdownOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesUIEvents IFunctionalitySubscriber<IProvidesUIEvents>.provider { get; set; }
#endif

        object GetCurrentDropObject(Transform rayOrigin)
        {
            IDroppable droppable;
            return m_Droppables.TryGetValue(rayOrigin, out droppable) ? droppable.GetDropObject() : null;
        }

        void SetCurrentDropReceiver(Transform rayOrigin, IDropReceiver dropReceiver)
        {
            if (dropReceiver == null)
                m_DropReceivers.Remove(rayOrigin);
            else
                m_DropReceivers[rayOrigin] = dropReceiver;
        }

        public IDropReceiver GetCurrentDropReceiver(Transform rayOrigin)
        {
            IDropReceiver dropReceiver;
            if (m_DropReceivers.TryGetValue(rayOrigin, out dropReceiver))
                return dropReceiver;

            return null;
        }

        public void OnRayEntered(GameObject gameObject, RayEventData eventData)
        {
            var dropReceiver = ComponentUtils<IDropReceiver>.GetComponent(gameObject);
            if (dropReceiver != null)
            {
                var rayOrigin = eventData.rayOrigin;
                if (dropReceiver.CanDrop(GetCurrentDropObject(rayOrigin)))
                {
                    dropReceiver.OnDropHoverStarted();
                    m_HoverObjects[rayOrigin] = gameObject;
                    SetCurrentDropReceiver(rayOrigin, dropReceiver);
                }
            }
        }

        public void OnRayExited(GameObject gameObject, RayEventData eventData)
        {
            if (!gameObject)
                return;

            var dropReceiver = ComponentUtils<IDropReceiver>.GetComponent(gameObject);
            if (dropReceiver != null)
            {
                var rayOrigin = eventData.rayOrigin;
                if (m_HoverObjects.Remove(rayOrigin))
                {
                    dropReceiver.OnDropHoverEnded();
                    SetCurrentDropReceiver(rayOrigin, null);
                }
            }
        }

        public void OnDragStarted(GameObject gameObject, RayEventData eventData)
        {
            var droppable = ComponentUtils<IDroppable>.GetComponent(gameObject);
            if (droppable != null)
                m_Droppables[eventData.rayOrigin] = droppable;
        }

        public void OnDragEnded(GameObject gameObject, RayEventData eventData)
        {
            var droppable = ComponentUtils<IDroppable>.GetComponent(gameObject);
            if (droppable != null)
            {
                var rayOrigin = eventData.rayOrigin;
                m_Droppables.Remove(rayOrigin);

                var dropReceiver = GetCurrentDropReceiver(rayOrigin);
                var dropObject = droppable.GetDropObject();
                if (dropReceiver != null && dropReceiver.CanDrop(dropObject))
                    dropReceiver.ReceiveDrop(droppable.GetDropObject());
            }
        }

        public void LoadModule() { }

        public void UnloadModule() { }

        public void Initialize()
        {
            this.SubscribeToRayEntered(OnRayEntered);
            this.SubscribeToRayExited(OnRayExited);
            this.SubscribeToDragStarted(OnDragStarted);
            this.SubscribeToDragEnded(OnDragEnded);
        }

        public void Shutdown()
        {
            this.UnsubscribeFromRayEntered(OnRayEntered);
            this.UnsubscribeFromRayExited(OnRayExited);
            this.UnsubscribeFromDragStarted(OnDragStarted);
            this.UnsubscribeFromDragEnded(OnDragEnded);
        }
    }
}
