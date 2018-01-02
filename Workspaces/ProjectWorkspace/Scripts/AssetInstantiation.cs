using System;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Data;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    internal static class AssetInstantiation
    {
        const string k_AudioClipAttachUndoLabel = "Add Audio Clip";
        const string k_AttachScriptUndoLabel = "Add Script";

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

    }
}
