using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class SpatialHashModule : MonoBehaviour, IDelayedInitializationModule
    {
        readonly List<Renderer> m_ChangedObjects = new List<Renderer>();
        SpatialHash<Renderer> m_SpatialHash;
        Coroutine m_UpdateCoroutine;

        public SpatialHash<Renderer> spatialHash { get { return m_SpatialHash; } }

        public Func<GameObject, bool> shouldExcludeObject { private get; set; }

        public int initializationOrder { get { return -3; } }
        public int shutdownOrder { get { return 0; } }

        public void LoadModule()
        {
            var moduleParent = ModuleLoaderCore.instance.GetModuleParent().transform;
            shouldExcludeObject = go => go.transform.IsChildOf(moduleParent);

            IUsesSpatialHashMethods.addToSpatialHash = AddObject;
            IUsesSpatialHashMethods.removeFromSpatialHash = RemoveObject;
        }

        public void UnloadModule() { }

        public void Initialize()
        {
            m_SpatialHash = new SpatialHash<Renderer>();
            SetupObjects();
            m_UpdateCoroutine = StartCoroutine(UpdateDynamicObjects());
        }

        public void Shutdown()
        {
            m_SpatialHash = null;
            StopCoroutine(m_UpdateCoroutine);
        }

        void SetupObjects()
        {
            var meshFilters = FindObjectsOfType<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh)
                {
                    if (shouldExcludeObject != null && shouldExcludeObject(mf.gameObject))
                        continue;

                    Renderer renderer = mf.GetComponent<Renderer>();
                    if (renderer)
                        spatialHash.AddObject(renderer, renderer.bounds);
                }
            }

            var skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                if (skinnedMeshRenderer.sharedMesh)
                {
                    if (shouldExcludeObject != null && shouldExcludeObject(skinnedMeshRenderer.gameObject))
                        continue;

                    spatialHash.AddObject(skinnedMeshRenderer, skinnedMeshRenderer.bounds);
                }
            }
        }

        IEnumerator UpdateDynamicObjects()
        {
            while (true)
            {
                m_ChangedObjects.Clear();

                // TODO AE 9/21/16: Hook updates of new objects that are created
                foreach (var obj in spatialHash.allObjects)
                {
                    if (!obj)
                    {
                        m_ChangedObjects.Add(obj);
                        continue;
                    }

                    if (obj.transform.hasChanged)
                    {
                        m_ChangedObjects.Add(obj);
                        obj.transform.hasChanged = false;
                    }
                }

                foreach (var changedObject in m_ChangedObjects)
                {
                    spatialHash.RemoveObject(changedObject);

                    if (changedObject)
                        spatialHash.AddObject(changedObject, changedObject.bounds);
                }

                yield return null;
            }
        }

        public void AddObject(GameObject gameObject)
        {
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                spatialHash.AddObject(renderer, renderer.bounds);
            }
        }

        public void RemoveObject(GameObject gameObject)
        {
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                spatialHash.RemoveObject(renderer);
            }
        }

        public Bounds GetMaxBounds()
        {
            return spatialHash.GetMaxBounds();
        }
    }
}
