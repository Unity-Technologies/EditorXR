using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Data;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Unity.Labs.EditorXR.Utilities
{
    static class AssetDropUtils
    {
        public static List<Material> activeMaterialClones = new List<Material>();

        public static readonly Dictionary<string, List<Type>> AssignmentDependencies
            = new Dictionary<string, List<Type>>
        {
            { "AnimationClip", new List<Type> { typeof(Animation) } },
            { "AudioClip", new List<Type> { typeof(AudioSource) } },
            { "VideoClip", new List<Type> { typeof(VideoPlayer) } },
            { "Material", new List<Type> { typeof(Renderer) } },
            { "Shader", new List<Type> { typeof(Material) } },
            { "PhysicMaterial", new List<Type> {typeof(Collider) } }
        };

        // dependency types to ignore when previewing asset assignment validity
        public static List<Type> AutoFillTypes = new List<Type>
        {
            typeof(Animation), typeof(AudioSource), typeof(VideoPlayer)
        };

        const string k_AssignAudioClipUndo = "Assign Audio Clip";
        const string k_AssignAnimationClipUndo = "Assign Animation Clip";
        const string k_AssignVideoClipUndo = "Assign Video Clip";
        const string k_AssignFontUndo = "Assign Font";
        const string k_AssignMaterialUndo = "Assign Material";
        const string k_AssignPhysicMaterialUndo = "Assign Physic Material";

        // TODO - make all these booleans options in the settings menu
        static bool s_CreatePlayerForClips = true;
        static bool s_AssignMultipleAnimationClips = true;

        internal static void AssignAnimationClip(Animation animation, AnimationClip clipAsset)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(animation, k_AssignAnimationClipUndo);
#endif

            if (animation.GetClipCount() > 0 && s_AssignMultipleAnimationClips)
                animation.AddClip(clipAsset, clipAsset.name);
            else
                animation.clip = clipAsset;
        }

        internal static Animation AssignAnimationClip(GameObject go, AssetData data)
        {
            var animation = ComponentUtils.GetOrAddIf<Animation>(go, s_CreatePlayerForClips);
            if (animation != null)
                AssignAnimationClip(animation, (AnimationClip)data.asset);

            return animation;
        }

        internal static void AssignAnimationClipAction(GameObject go, AssetData data)
        {
            AssignAnimationClip(go, data);
        }

        internal static AudioSource AttachAudioClip(GameObject go, AssetData data)
        {
            var source = ComponentUtils.GetOrAddIf<AudioSource>(go, s_CreatePlayerForClips);
            if (source != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(source, k_AssignAudioClipUndo);
#endif
                source.clip = (AudioClip)data.asset;
            }

            return source;
        }

        internal static void AudioClipAction(GameObject go, AssetData data)
        {
            AttachAudioClip(go, data);
        }

        internal static VideoPlayer AttachVideoClip(GameObject go, AssetData data)
        {
            var player = ComponentUtils.GetOrAddIf<VideoPlayer>(go, s_CreatePlayerForClips);
            if (player != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(player, k_AssignVideoClipUndo);
#endif
                player.clip = (VideoClip)data.asset;
            }

            return player;
        }

        internal static void VideoClipAction(GameObject go, AssetData data)
        {
            AttachVideoClip(go, data);
        }

        internal static GameObject AttachScript(GameObject go, AssetData data)
        {
#if UNITY_EDITOR
            var script = (MonoScript)data.asset;
            var type = script.GetClass();
            UnityEditor.Undo.AddComponent(go, type);
#endif
            return go;
        }

        internal static void AttachScriptAction(GameObject go, AssetData data)
        {
            AttachScript(go, data);
        }

        internal static Renderer AssignMaterial(GameObject go, AssetData data)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(go, k_AssignMaterialUndo);
#endif
                renderer.sharedMaterial = (Material)data.asset;
            }

            return renderer;
        }

        internal static void AssignMaterialAction(GameObject go, AssetData data)
        {
            AssignMaterial(go, data);
        }

        internal static Material AssignShader(GameObject go, AssetData data)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(go, k_AssignMaterialUndo);
#endif

                // copy the material before applying shader to the instance
                // this prevents the warning about leaking materials
                var materialCopy = MaterialUtils.CloneMaterials(renderer)[0];
                var shader = (Shader)data.asset;
                materialCopy.shader = shader;
                renderer.sharedMaterial = materialCopy;

                activeMaterialClones.Add(materialCopy);
                return materialCopy;
            }

            return null;
        }

        internal static void AssignShaderAction(GameObject go, AssetData data)
        {
            AssignShader(go, data);
        }

        internal static PhysicMaterial AssignPhysicMaterial(GameObject go, AssetData data)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                var material = (PhysicMaterial)data.asset;
                AssignPhysicMaterial(collider, material);
                return collider.material;
            }

            return null;
        }

        internal static void AssignPhysicMaterialAction(GameObject go, AssetData data)
        {
            AssignPhysicMaterial(go, data);
        }

        internal static void AssignPhysicMaterial(Collider collider, PhysicMaterial material)
        {
#if UNITY_EDITOR
#endif
            UnityEditor.Undo.RecordObject(collider, k_AssignPhysicMaterialUndo);
            collider.material = material;
        }

        internal static Font AssignFontOnChildren(GameObject go, AssetData data)
        {
            var text = go.GetComponentInChildren<TextMesh>();

            if (text != null)
            {
                var font = (Font)data.asset;

#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(go, k_AssignFontUndo);
#endif

                text.font = font;

                return font;
            }

            return null;
        }

        internal static void AssignFontAction(GameObject go, AssetData data)
        {
            AssignFontOnChildren(go, data);
        }
    }
}
