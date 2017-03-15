using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class SceneObjectModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrSceneObjectModule = evr.m_SceneObjectModule;

				var placeObjects = obj as IPlaceObject;
				if (placeObjects != null)
					placeObjects.placeObject = evrSceneObjectModule.PlaceSceneObject;

				var deleteSceneObjects = obj as IDeleteSceneObject;
				if (deleteSceneObjects != null)
					deleteSceneObjects.deleteSceneObject = evrSceneObjectModule.DeleteSceneObject;
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}

}
