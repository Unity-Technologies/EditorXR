using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEditor.Experimental.EditorVR.Data;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    public static class AssetDropUtils
    {
        public static List<Material> activeMaterialClones = new List<Material>();

        // null means assignable to anything
        public static Dictionary<string, List<Type>> AssignmentDependencies
            = new Dictionary<string, List<Type>>()
        {
            { "AnimationClip", MakeList(typeof(Animation)) },
            { "AnimationClip", new List<Type> { (typeof(Animation)) } },
            { "AudioClip", MakeList(typeof(AudioSource)) },
            { "VideoClip", MakeList(typeof(VideoPlayer)) },
            { "Material", MakeList(typeof(Renderer)) },
            { "Shader", MakeList(typeof(Material)) },
            { "PhysicMaterial", MakeList(typeof(Collider)) },
            { "Model", null },
            { "Prefab", null },
            { "Script", null },
        };

        const string k_AssignAudioClipUndo = "Assign Audio Clip";
        const string k_AssignAnimationClipUndo = "Assign Animation Clip";
        const string k_AssignVideoClipUndo = "Assign Video Clip";
        const string k_AssignFontUndo = "Assign Font";
        const string k_AttachScriptUndo = "Assign Script";
        const string k_AssignMaterialUndo = "Assign Material";
        const string k_AssignPhysicMaterialUndo = "Assign Physic Material";
        const string k_AssignMaterialShaderUndo = "Assign Material Shader";

        // TODO - make all these booleans options in the settings menu
        static bool m_CreatePlayerForClips = true;
        static bool m_AssignMultipleAnimationClips = true;
        static bool m_SwapDefaultAnimationClips = true;
        static bool m_InstanceMaterialOnShaderAssign = true;

        static List<Type> MakeList(params Type[] types)
        {
            return new List<Type>(types);
        }

        internal static void AssignAnimationClip(Animation animation, AnimationClip clipAsset)
        {
            Undo.RecordObject(animation, k_AssignAnimationClipUndo);

            if (animation.GetClipCount() > 0 && m_AssignMultipleAnimationClips)
            {
                if (m_SwapDefaultAnimationClips)
                {
                    var tempClip = animation.clip;
                    animation.RemoveClip(animation.clip);
                    animation.clip = clipAsset;
                    animation.AddClip(tempClip, tempClip.name);
                }
                else
                {
                    animation.AddClip(clipAsset, clipAsset.name);
                }
            }
            else
            {
                animation.clip = clipAsset;
            }
        }

        internal static Animation AssignAnimationClip(GameObject go, AssetData data)
        {
            var animation = ComponentUtils.GetOrAddIf<Animation>(go, m_CreatePlayerForClips);
            if (animation != null)
                AssignAnimationClip(animation, (AnimationClip)data.asset);

            return animation;
        }

        internal static AudioSource AttachAudioClip(GameObject go, AssetData data)
        {
            var source = ComponentUtils.GetOrAddIf<AudioSource>(go, m_CreatePlayerForClips);
            if (source != null)
            {
                Undo.RecordObject(source, k_AssignAudioClipUndo);
                source.clip = (AudioClip)data.asset;
            }

            return source;
        }

        internal static VideoPlayer AttachVideoClip(GameObject go, AssetData data)
        {
            var player = ComponentUtils.GetOrAddIf<VideoPlayer>(go, m_CreatePlayerForClips);
            if (player != null)
            {
                Undo.RecordObject(player, k_AssignVideoClipUndo);
                player.clip = (VideoClip)data.asset;
            }

            return player;
        }

        internal static GameObject AttachScript(GameObject go, AssetData data)
        {
            var script = (MonoScript)data.asset;
            var type = script.GetClass();
            Undo.AddComponent(go, type);
            return go;
        }

        internal static Renderer AssignMaterial(GameObject go, AssetData data)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Undo.RecordObject(go, k_AssignMaterialUndo);
                renderer.sharedMaterial = (Material)data.asset;
            }

            return renderer;
        }

        internal static Material AssignMaterialShader(GameObject go, AssetData data)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Undo.RecordObject(go, k_AssignMaterialUndo);

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

        internal static PhysicMaterial AssignColliderPhysicMaterial(GameObject go, AssetData data)
        {
            var collider = go.GetComponent<Collider>();

            if (collider != null)
            {
                var material = (PhysicMaterial)data.asset;
                AssignColliderPhysicMaterial(collider, material);

                return collider.material;
            }

            return null;
        }

        internal static void AssignColliderPhysicMaterial(Collider collider, PhysicMaterial material)
        {
            Undo.RecordObject(collider, k_AssignPhysicMaterialUndo);
            collider.material = material;
        }

        internal static Font AssignFontOnChildren(GameObject go, AssetData data)
        {
            var text = go.GetComponentInChildren<TextMesh>();

            if (text != null)
            {
                var font = (Font)data.asset;
                Undo.RecordObject(go, k_AssignFontUndo);
                text.font = font;

                return font;
            }

            return null;
        }

    }
}
