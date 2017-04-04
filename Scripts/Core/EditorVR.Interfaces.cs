#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class Interfaces : Nested
		{
			readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

			event IConnectInterfacesMethods.ConnectInterfacesDelegate connectInterfaces;
			event IConnectInterfacesMethods.DisonnectInterfacesDelegate disconnectInterfaces;

			public Interfaces()
			{
				IConnectInterfacesMethods.connectInterfaces = ConnectInterfaces;
				IConnectInterfacesMethods.disconnectInterfaces = DisconnectInterfaces;
			}

			internal void AttachInterfaceConnectors(object obj)
			{
				var connector = obj as IInterfaceConnector;
				if (connector != null)
				{
					connectInterfaces += connector.ConnectInterface;
					disconnectInterfaces += connector.DisconnectInterface;
				}
			}

			internal void ConnectInterfaces(object obj, InputDevice device)
			{
				Transform rayOrigin = null;
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.inputDevice == device);
				if (deviceData != null)
					rayOrigin = deviceData.rayOrigin;

				ConnectInterfaces(obj, rayOrigin);
			}

			internal void ConnectInterfaces(object obj, Transform rayOrigin = null)
			{
				if (!m_ConnectedInterfaces.Add(obj))
					return;

				if (connectInterfaces != null)
					connectInterfaces(obj, rayOrigin);
			}

			internal void DisconnectInterfaces(object obj)
			{
				m_ConnectedInterfaces.Remove(obj);

				if (disconnectInterfaces != null)
					disconnectInterfaces(obj);
			}
		}
	}
}

#endif
