using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SceneObjectModuleConnector : Nested, ILateBindInterfaceMethods<SceneObjectModule>
        {
            public void LateBindInterfaceMethods(SceneObjectModule provider)
            {
                IDeleteSceneObjectMethods.deleteSceneObject = provider.DeleteSceneObject;
                IPlaceSceneObjectMethods.placeSceneObject = provider.PlaceSceneObject;
                IPlaceSceneObjectsMethods.placeSceneObjects = provider.PlaceSceneObjects;
            }
        }
    }
}