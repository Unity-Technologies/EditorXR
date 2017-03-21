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
			const byte k_MinStencilRef = 5;
			readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

			event ConnectInterfacesDelegate m_ConnectInterfaces;
			event Action<object> m_DisconnectInterfaces;

			internal void AttachInterfaceConnectors(object obj)
			{
				var connector = obj as IInterfaceConnector;
				if (connector != null)
				{
					m_ConnectInterfaces += connector.ConnectInterface;
					m_DisconnectInterfaces += connector.DisconnectInterface;
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

				var connectInterfaces = obj as IConnectInterfaces;
				if (connectInterfaces != null)
					connectInterfaces.connectInterfaces = ConnectInterfaces;

				if (m_ConnectedInterfaces != null)
					m_ConnectInterfaces(obj, rayOrigin);
			}

			internal void DisconnectInterfaces(object obj)
			{
				m_ConnectedInterfaces.Remove(obj);

				if (m_DisconnectInterfaces != null)
					m_DisconnectInterfaces(obj);
			}
		}
	}
}

#endif
