using System;
using Unity.EditorXR.Interfaces;
using Unity.XRTools.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR
{
    [ActionMenuItem("Delete", ActionMenuItemAttribute.DefaultActionSectionName, 7)]
    [SpatialMenuItem("Delete", "Actions", "Delete the selected object")]
    sealed class Delete : BaseAction, IUsesDeleteSceneObject
    {
        public Action<GameObject> addToSpatialHash { private get; set; }
        public Action<GameObject> removeFromSpatialHash { private get; set; }

#if !FI_AUTOFILL
        IProvidesDeleteSceneObject IFunctionalitySubscriber<IProvidesDeleteSceneObject>.provider { get; set; }
#endif

        public override void ExecuteAction()
        {
            var gameObjects = Selection.gameObjects;
            foreach (var go in gameObjects)
            {
                this.DeleteSceneObject(go);
            }

#if UNITY_EDITOR
            UnityEditor.Undo.IncrementCurrentGroup();
#endif

            Selection.activeGameObject = null;
        }
    }
}
