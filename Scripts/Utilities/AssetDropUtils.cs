using System;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Data;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    internal static class AssetDropUtils
    {
        const string k_AudioClipAttachUndoLabel = "Add Audio Clip";
        const string k_AssignFontUndoLabel = "Assign Font";
        const string k_AttachScriptUndoLabel = "Add Script";
        const string k_AssignMaterialUndoLabel = "Assign Material";
        const string k_AssignPhysicMaterialUndoLabel = "Assign Physic Material";
        const string k_AssignMaterialShaderUndoLabel = "Assign Material Shader";

        internal static AudioSource AttachAudioClip(GameObject go, AssetData data)
        {
            var source = go.GetComponent<AudioSource>();
            if (source == null)
                source = Undo.AddComponent<AudioSource>(go);
            
            source.clip = (AudioClip)data.asset;

            Undo.IncrementCurrentGroup();
            return source;
        }

        internal static Type AttachScript(GameObject go, AssetData data)
        {
            var script = (MonoScript)data.asset;
            var type = script.GetClass();
            Undo.AddComponent(go, type);
            return type;
        }

        internal static Material AssignMaterial(GameObject go, AssetData data)
        {
            var renderer = go.GetComponent<Renderer>();

            if (renderer != null)
            {
                Undo.RecordObject(go, k_AssignMaterialUndoLabel);
                renderer.sharedMaterial = (Material)data.asset;
                Undo.IncrementCurrentGroup();
                return renderer.sharedMaterial;
            }

            return null;
        }

        internal static Shader AssignMaterialShader(GameObject go, AssetData data)
        {
            var renderer = go.GetComponent<Renderer>();

            if (renderer != null)
            {
                Undo.RecordObject(go, k_AssignMaterialUndoLabel);
                // this warns that we're leaking materials into the scene,
                // and creates a new instance, but we don't want to change
                // the shader on the shared material here.
                var shader = (Shader)data.asset;
                renderer.material.shader = shader;
                Undo.IncrementCurrentGroup();
                return shader;
            }

            return null;
        }

        internal static PhysicMaterial AssignColliderPhysicMaterial(GameObject go, AssetData data)
        {
            var collider = go.GetComponent<Collider>();

            if(collider != null)
            {
                Undo.RecordObject(go, k_AssignPhysicMaterialUndoLabel);
                collider.material = (PhysicMaterial)data.asset;
                Undo.IncrementCurrentGroup();

                return collider.material;
            }

            return null;
        }

        internal static Font AssignFontOnChildren(GameObject go, AssetData data)
        {
            var text = go.GetComponentInChildren<TextMesh>();

            if (text != null)
            {
                var font = (Font)data.asset;
                Undo.RecordObject(go, k_AssignFontUndoLabel);
                text.font = font;
                Undo.IncrementCurrentGroup();
                return font;
            }

            return null;
        }

    }
}
