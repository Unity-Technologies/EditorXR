using System;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Data;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    internal static class AssetInstantiation
    {
        const string k_AudioClipAttachUndoLabel = "Add Audio Clip";
        const string k_AttachScriptUndoLabel = "Add Script";
        const string k_AssignMaterialUndoLabel = "Assign Material";
        const string k_AssignPhysicMaterialUndoLabel = "Assign Physic Material";

        internal static AudioSource AttachAudioClip(GameObject go, AssetData data)
        {
            Undo.RecordObject(go, k_AudioClipAttachUndoLabel);

            var source = go.GetComponent<AudioSource>();
            if (source == null)
                source = go.AddComponent<AudioSource>();
            
            source.clip = (AudioClip)data.asset;

            Undo.IncrementCurrentGroup();
            return source;
        }

        internal static Type AttachScript(GameObject go, AssetData data)
        {
            Undo.RecordObject(go, k_AttachScriptUndoLabel);

            var script = (MonoScript)data.asset;
            var type = script.GetClass();
            var component = go.AddComponent(type);

            Undo.IncrementCurrentGroup();
            return type;
        }

        internal static Material SwapMaterial(GameObject go, AssetData data)
        {
            var renderer = go.GetComponent<Renderer>();

            if (renderer != null)
            {
                Undo.RecordObject(go, k_AttachScriptUndoLabel);
                renderer.material = (Material)data.asset;
                Undo.IncrementCurrentGroup();
                return renderer.material;
            }

            return null;
        }

        internal static PhysicMaterial AssignColliderPhysicMaterial(GameObject go, AssetData data)
        {
            var collider = go.GetComponent<Collider>();

            if(collider != null)
            {
                Undo.RecordObject(go, k_AttachScriptUndoLabel);
                collider.material = (PhysicMaterial)data.asset;
                Undo.IncrementCurrentGroup();

                return collider.material;
            }

            return null;
        }

    }
}
