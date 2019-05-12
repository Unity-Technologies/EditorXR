#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class DirectSelection : Nested, IInterfaceConnector
        {
            readonly Dictionary<Transform, DirectSelectionData> m_DirectSelections = new Dictionary<Transform, DirectSelectionData>();
            readonly Dictionary<Transform, HashSet<Transform>> m_GrabbedObjects = new Dictionary<Transform, HashSet<Transform>>();
            readonly List<IGrabObjects> m_ObjectGrabbers = new List<IGrabObjects>();
            readonly List<IUsesDirectSelection> m_DirectSelectionUsers = new List<IUsesDirectSelection>();
            readonly List<ITwoHandedScaler> m_TwoHandedScalers = new List<ITwoHandedScaler>();

            IntersectionModule m_IntersectionModule;

            public event Action<Transform, HashSet<Transform>> objectsGrabbed;
            public event Action<Transform, Transform[]> objectsDropped;
            public event Action<Transform, Transform> objectsTransferred;

            public DirectSelection()
            {
                IUsesDirectSelectionMethods.getDirectSelection = () => m_DirectSelections;
                IUsesDirectSelectionMethods.resetDirectSelectionState = ResetDirectSelectionState;

                ICanGrabObjectMethods.canGrabObject = CanGrabObject;

                IUsesPointerMethods.getPointerLength = GetPointerLength;
            }

            public void ConnectInterface(object target, object userData = null)
            {
                var grabObjects = target as IGrabObjects;
                if (grabObjects != null)
                {
                    m_ObjectGrabbers.Add(grabObjects);
                    grabObjects.objectsGrabbed += OnObjectsGrabbed;
                    grabObjects.objectsDropped += OnObjectsDropped;
                    grabObjects.objectsTransferred += OnObjectsTransferred;
                }

                var usesDirectSelection = target as IUsesDirectSelection;
                if (usesDirectSelection != null)
                    m_DirectSelectionUsers.Add(usesDirectSelection);

                var twoHandedScaler = target as ITwoHandedScaler;
                if (twoHandedScaler != null)
                    m_TwoHandedScalers.Add(twoHandedScaler);
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var grabObjects = target as IGrabObjects;
                if (grabObjects != null)
                {
                    m_ObjectGrabbers.Remove(grabObjects);
                    grabObjects.objectsGrabbed -= OnObjectsGrabbed;
                    grabObjects.objectsDropped -= OnObjectsDropped;
                    grabObjects.objectsTransferred -= OnObjectsTransferred;
                }
            }

            // NOTE: This is for the length of the pointer object, not the length of the ray coming out of the pointer
            internal static float GetPointerLength(Transform rayOrigin)
            {
                var length = 0f;

                // Check if this is a MiniWorldRay
                MiniWorlds.MiniWorldRay ray;
                if (evr.GetNestedModule<MiniWorlds>().rays.TryGetValue(rayOrigin, out ray))
                    rayOrigin = ray.originalRayOrigin;

                DefaultProxyRay dpr;
                if (evr.GetNestedModule<Rays>().defaultRays.TryGetValue(rayOrigin, out dpr))
                {
                    length = dpr.pointerLength;

                    // If this is a MiniWorldRay, scale the pointer length to the correct size relative to MiniWorld objects
                    if (ray != null)
                    {
                        var miniWorld = ray.miniWorld;

                        // As the miniworld gets smaller, the ray length grows, hence lossyScale.Inverse().
                        // Assume that both transforms have uniform scale, so we just need .x
                        length *= miniWorld.referenceTransform.TransformVector(miniWorld.miniWorldTransform.lossyScale.Inverse()).x;
                    }
                }

                return length;
            }

            internal bool IsHovering(Transform rayOrigin)
            {
                return m_DirectSelections.ContainsKey(rayOrigin);
            }

            internal bool IsScaling(Transform rayOrigin)
            {
                return m_TwoHandedScalers.Any(twoHandedScaler => twoHandedScaler.IsTwoHandedScaling(rayOrigin));
            }

            internal void UpdateDirectSelection()
            {
                m_DirectSelections.Clear();

                foreach (var deviceData in evr.m_DeviceData)
                {
                    var proxy = deviceData.proxy;
                    if (!proxy.active)
                        continue;

                    var rayOrigin = deviceData.rayOrigin;
                    Vector3 contactPoint;
                    var obj = GetDirectSelectionForRayOrigin(rayOrigin, out contactPoint);
                    if (obj && !obj.CompareTag(k_VRPlayerTag))
                    {
                        m_DirectSelections[rayOrigin] = new DirectSelectionData
                        {
                            gameObject = obj,
                            contactPoint = contactPoint
                        };
                    }
                }

                foreach (var ray in evr.GetNestedModule<MiniWorlds>().rays)
                {
                    var rayOrigin = ray.Key;
                    Vector3 contactPoint;
                    var go = GetDirectSelectionForRayOrigin(rayOrigin, out contactPoint);
                    if (go != null)
                    {
                        m_DirectSelections[rayOrigin] = new DirectSelectionData
                        {
                            gameObject = go,
                            contactPoint = contactPoint
                        };
                    }
                }
            }

            GameObject GetDirectSelectionForRayOrigin(Transform rayOrigin, out Vector3 contactPoint)
            {
                if (m_IntersectionModule == null)
                    m_IntersectionModule = evr.GetModule<IntersectionModule>();

                var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();

                var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester, out contactPoint);
                if (renderer)
                    return renderer.gameObject;

                return null;
            }

            static bool CanGrabObject(GameObject selection, Transform rayOrigin)
            {
                if (selection.CompareTag(k_VRPlayerTag) && !evr.GetNestedModule<MiniWorlds>().rays.ContainsKey(rayOrigin))
                    return false;

                return true;
            }

            void OnObjectsGrabbed(Transform rayOrigin, HashSet<Transform> grabbedObjects)
            {
                HashSet<Transform> objects;
                if (!m_GrabbedObjects.TryGetValue(rayOrigin, out objects))
                {
                    objects = new HashSet<Transform>();
                    m_GrabbedObjects[rayOrigin] = objects;
                }

                objects.UnionWith(grabbedObjects);

                // Detach the player head model so that it is not affected by its parent transform
                foreach (var grabbedObject in grabbedObjects)
                {
                    if (grabbedObject.CompareTag(k_VRPlayerTag))
                    {
                        grabbedObject.hideFlags = HideFlags.None;
                        grabbedObject.transform.parent = null;
                    }
                }

                if (objectsGrabbed != null)
                    objectsGrabbed(rayOrigin, grabbedObjects);
            }

            void OnObjectsDropped(Transform rayOrigin, Transform[] grabbedObjects)
            {
                HashSet<Transform> objects;
                if (!m_GrabbedObjects.TryGetValue(rayOrigin, out objects))
                    return;

                var sceneObjectModule = evr.GetModule<SceneObjectModule>();
                var viewer = evr.GetNestedModule<Viewer>();
                var miniWorlds = evr.GetNestedModule<MiniWorlds>();
                var eventObjects = new List<Transform>();
                foreach (var grabbedObject in grabbedObjects)
                {
                    objects.Remove(grabbedObject);

                    // Dropping the player head updates the camera rig position
                    if (grabbedObject.CompareTag(k_VRPlayerTag))
                        Viewer.DropPlayerHead(grabbedObject);
                    else if (viewer.IsOverShoulder(rayOrigin) && !miniWorlds.rays.ContainsKey(rayOrigin))
                        sceneObjectModule.DeleteSceneObject(grabbedObject.gameObject);
                    else
                        eventObjects.Add(grabbedObject);
                }

                if (objects.Count == 0)
                    m_GrabbedObjects.Remove(rayOrigin);

                if (objectsDropped != null)
                    objectsDropped(rayOrigin, eventObjects.ToArray());
            }

            void OnObjectsTransferred(Transform srcRayOrigin, Transform destRayOrigin)
            {
                m_GrabbedObjects[destRayOrigin] = m_GrabbedObjects[srcRayOrigin];
                m_GrabbedObjects.Remove(srcRayOrigin);

                if (objectsTransferred != null)
                    objectsTransferred(srcRayOrigin, destRayOrigin);
            }

            public HashSet<Transform> GetHeldObjects(Transform rayOrigin)
            {
                HashSet<Transform> objects;
                return m_GrabbedObjects.TryGetValue(rayOrigin, out objects) ? objects : null;
            }

            public void SuspendGrabbers(Node node)
            {
                foreach (var grabber in m_ObjectGrabbers)
                {
                    grabber.Suspend(node);
                }
            }

            public void ResumeGrabbers(Node node)
            {
                foreach (var grabber in m_ObjectGrabbers)
                {
                    grabber.Resume(node);
                }
            }

            public void DropHeldObjects(Node node)
            {
                foreach (var grabber in m_ObjectGrabbers)
                {
                    grabber.DropHeldObjects(node);
                }
            }

            public void TransferHeldObjects(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset = default(Vector3))
            {
                foreach (var grabber in m_ObjectGrabbers)
                {
                    grabber.TransferHeldObjects(rayOrigin, destRayOrigin, deltaOffset);
                }
            }

            void ResetDirectSelectionState()
            {
                foreach (var usesDirectSelection in m_DirectSelectionUsers)
                {
                    usesDirectSelection.OnResetDirectSelectionState();
                }
            }
        }
    }
}
#endif
