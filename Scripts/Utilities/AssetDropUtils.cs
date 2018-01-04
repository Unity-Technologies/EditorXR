using System;
using UnityEngine;
using UnityEngine.Video;
using UnityEditor.Experimental.EditorVR.Data;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    internal static class AssetDropUtils
    {
        const string k_AssignAudioClipUndo = "Assign Audio Clip";
        const string k_AssignAnimationClipUndo = "Assign Animation Clip";
        const string k_AssignVideoClipUndo = "Assign Video Clip";
        const string k_AssignFontUndo = "Assign Font";
        const string k_AttachScriptUndo = "Assign Script";
        const string k_AssignMaterialUndo = "Assign Material";
        const string k_AssignPhysicMaterialUndo = "Assign Physic Material";
        const string k_AssignMaterialShaderUndo = "Assign Material Shader";

        internal static AnimationClip AttachAnimationClip(GameObject go, AssetData data)
        {
            var animation = go.GetComponent<Animation>();
            if (animation == null)
                animation = Undo.AddComponent<Animation>(go);

            Undo.RecordObject(animation, k_AssignAnimationClipUndo);
            animation.clip = (AnimationClip)data.asset;

            Undo.IncrementCurrentGroup();
            return animation.clip;
        }

        internal static AudioClip AttachAudioClip(GameObject go, AssetData data)
        {
            var source = go.GetComponent<AudioSource>();
            if (source == null)
                source = Undo.AddComponent<AudioSource>(go);

            Undo.RecordObject(source, k_AssignAudioClipUndo);
            source.clip = (AudioClip)data.asset;

            Undo.IncrementCurrentGroup();
            return source.clip;
        }

        internal static VideoClip AttachVideoClip(GameObject go, AssetData data)
        {
            var player = go.GetComponent<VideoPlayer>();
            if (player == null)
                player = Undo.AddComponent<VideoPlayer>(go);

            Undo.RecordObject(player, k_AssignVideoClipUndo);
            player.clip = (VideoClip)data.asset;

            Undo.IncrementCurrentGroup();
            return player.clip;
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
                Undo.RecordObject(go, k_AssignMaterialUndo);
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
                Undo.RecordObject(go, k_AssignMaterialUndo);
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
                Undo.RecordObject(go, k_AssignPhysicMaterialUndo);
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
                Undo.RecordObject(go, k_AssignFontUndo);
                text.font = font;
                Undo.IncrementCurrentGroup();
                return font;
            }

            return null;
        }

    }
}
