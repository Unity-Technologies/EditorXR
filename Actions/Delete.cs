
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Delete", ActionMenuItemAttribute.DefaultActionSectionName, 7)]
    sealed class Delete : BaseAction, IDeleteSceneObject
    {
        public Action<GameObject> addToSpatialHash { private get; set; }
        public Action<GameObject> removeFromSpatialHash { private get; set; }

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

