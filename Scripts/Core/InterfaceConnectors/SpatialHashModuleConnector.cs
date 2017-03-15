using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class SpatialHashModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrSpatialHashModule = evr.m_SpatialHashModule;

				var spatialHash = obj as IUsesSpatialHash;
				if (spatialHash != null)
				{
					spatialHash.addToSpatialHash = evrSpatialHashModule.AddObject;
					spatialHash.removeFromSpatialHash = evrSpatialHashModule.RemoveObject;
				}
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}

}
