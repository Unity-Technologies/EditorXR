#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		[SerializeField]
		MainMenuActivator m_MainMenuActivatorPrefab;

		[SerializeField]
		PinnedToolButton m_PinnedToolButtonPrefab;

		class Menus : Nested, IInterfaceConnector, ILateBindInterfaceMethods<Tools>
		{
			[Flags]
			internal enum MenuHideFlags
			{
				Hidden = 1 << 0,
				OverUI = 1 << 1
			}

			const float k_MenuHideMargin = 0.8f;

			readonly Dictionary<Type, ISettingsMenuProvider> m_SettingsMenuProviders = new Dictionary<Type, ISettingsMenuProvider>();
			List<Type> m_MainMenuTools;

			// Local method use only -- created here to reduce garbage collection
			readonly List<DeviceData> m_ActiveDeviceData = new List<DeviceData>();

			public Menus()
			{
				IInstantiateMenuUIMethods.instantiateMenuUI = InstantiateMenuUI;
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var settingsMenuProvider = obj as ISettingsMenuProvider;
				if (settingsMenuProvider != null)
					m_SettingsMenuProviders[obj.GetType()] = settingsMenuProvider;

				var mainMenu = obj as IMainMenu;
				if (mainMenu != null)
				{
					mainMenu.menuTools = m_MainMenuTools;
					mainMenu.menuWorkspaces = WorkspaceModule.workspaceTypes;
					mainMenu.settingsMenuProviders = m_SettingsMenuProviders;
				}
			}

			public void DisconnectInterface(object obj)
			{
				var settingsMenuProvider = obj as ISettingsMenuProvider;
				if (settingsMenuProvider != null)
					m_SettingsMenuProviders.Remove(obj.GetType());
			}

			public void LateBindInterfaceMethods(Tools provider)
			{
				m_MainMenuTools = provider.allTools.Where(t => !Tools.IsDefaultTool(t)).ToList(); // Don't show tools that can't be selected/toggled
			}

			static void UpdateAlternateMenuForDevice(DeviceData deviceData)
			{
				var alternateMenu = deviceData.alternateMenu;
				alternateMenu.visible = deviceData.menuHideFlags[alternateMenu] == 0 && !(deviceData.currentTool is IExclusiveMode);

				// Move the activator button to an alternate position if the alternate menu will be shown
				var mainMenuActivator = deviceData.mainMenuActivator;
				if (mainMenuActivator != null)
					mainMenuActivator.activatorButtonMoveAway = alternateMenu.visible;
			}

			internal void UpdateMenuVisibilities()
			{
				m_ActiveDeviceData.Clear();
				Rays.ForEachProxyDevice(deviceData =>
				{
					m_ActiveDeviceData.Add(deviceData);
				});

				// Reconcile conflicts because menus on the same device can visually overlay each other
				for (int i = 0; i < m_ActiveDeviceData.Count; i++)
				{
					var deviceData = m_ActiveDeviceData[i];
					var alternateMenu = deviceData.alternateMenu;
					var mainMenu = deviceData.mainMenu;
					var customMenu = deviceData.customMenu;
					var menuHideFlags = deviceData.menuHideFlags;

					// Move alternate menu to another device if it conflicts with main or custom menu
					if (alternateMenu != null && (menuHideFlags[mainMenu] == 0 || (customMenu != null && menuHideFlags[customMenu] == 0)) && menuHideFlags[alternateMenu] == 0)
					{
						for (int j = 0; j < m_ActiveDeviceData.Count; j++)
						{
							var otherDeviceData = m_ActiveDeviceData[j];
							if (otherDeviceData == deviceData)
								continue;

							var otherCustomMenu = otherDeviceData.customMenu;
							var otherHideFlags = otherDeviceData.menuHideFlags;
							otherHideFlags[otherDeviceData.alternateMenu] &= ~MenuHideFlags.Hidden;
							otherHideFlags[otherDeviceData.mainMenu] |= MenuHideFlags.Hidden;

							if (otherCustomMenu != null)
								otherHideFlags[otherCustomMenu] |= MenuHideFlags.Hidden;
						}

						menuHideFlags[alternateMenu] |= MenuHideFlags.Hidden;
					}

					if (customMenu != null && menuHideFlags[mainMenu] == 0 && menuHideFlags[customMenu] == 0)
					{
						menuHideFlags[customMenu] |= MenuHideFlags.Hidden;
					}
				}

				// Apply state to UI visibility
				Rays.ForEachProxyDevice(deviceData =>
				{
					var mainMenu = deviceData.mainMenu;
					mainMenu.visible = deviceData.menuHideFlags[mainMenu] == 0;

					var customMenu = deviceData.customMenu;
					if (customMenu != null)
						customMenu.visible = deviceData.menuHideFlags[customMenu] == 0;

					UpdateAlternateMenuForDevice(deviceData);
					Rays.UpdateRayForDevice(deviceData, deviceData.rayOrigin);
				});

				evr.GetModule<DeviceInputModule>().UpdatePlayerHandleMaps();
			}

			internal static void OnUIHoverStarted(GameObject go, RayEventData rayEventData)
			{
				OnHover(go, rayEventData, false);
			}

			internal static void OnUIHovering(GameObject go, RayEventData rayEventData)
			{
				OnHover(go, rayEventData, false);
			}

			internal static void OnUIHoverEnded(GameObject go, RayEventData rayEventData)
			{
				OnHover(go, rayEventData, true);
			}

			internal static void OnHover(GameObject go, RayEventData rayEventData, bool ended)
			{
				if (go == evr.gameObject)
					return;

				var rayOrigin = rayEventData.rayOrigin;
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
				{
					var scaledPointerDistance = rayEventData.pointerCurrentRaycast.distance / Viewer.GetViewerScale();
					var isManipulator = go.GetComponentInParent<IManipulator>() != null;
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						if (ended || isManipulator || scaledPointerDistance > menu.hideDistance + k_MenuHideMargin)
							deviceData.menuHideFlags[menu] &= ~MenuHideFlags.OverUI;
						else
							deviceData.menuHideFlags[menu] |= MenuHideFlags.OverUI;
					}
				}
			}

			internal static void UpdateAlternateMenuOnSelectionChanged(Transform rayOrigin)
			{
				SetAlternateMenuVisibility(rayOrigin, Selection.gameObjects.Length > 0);
			}

			internal static void SetAlternateMenuVisibility(Transform rayOrigin, bool visible)
			{
				Rays.ForEachProxyDevice(deviceData =>
				{
					var alternateMenu = deviceData.alternateMenu;
					if (alternateMenu != null)
					{
						var flags = deviceData.menuHideFlags[alternateMenu];
						deviceData.menuHideFlags[alternateMenu] = (deviceData.rayOrigin == rayOrigin) && visible ? flags & ~MenuHideFlags.Hidden : flags | MenuHideFlags.Hidden;
					}
				});
			}

			internal static void OnMainMenuActivatorSelected(Transform rayOrigin, Transform targetRayOrigin)
			{
				foreach (var deviceData in evr.m_DeviceData)
				{
					var mainMenu = deviceData.mainMenu;
					var menuHideFlags = deviceData.menuHideFlags;
					if (mainMenu != null)
					{
						if (deviceData.rayOrigin == rayOrigin)
						{
							menuHideFlags[mainMenu] ^= MenuHideFlags.Hidden;

							var customMenu = deviceData.customMenu;
							if (customMenu != null)
								menuHideFlags[customMenu] &= ~MenuHideFlags.Hidden;

							mainMenu.targetRayOrigin = targetRayOrigin;
						}
						else
						{
							menuHideFlags[mainMenu] |= MenuHideFlags.Hidden;
						}
					}
				}
			}

			static GameObject InstantiateMenuUI(Transform rayOrigin, IMenu prefab)
			{
				var ui = evr.GetNestedModule<UI>();
				GameObject go = null;
				Rays.ForEachProxyDevice(deviceData =>
				{
					var proxy = deviceData.proxy;
					var otherRayOrigin = deviceData.rayOrigin;
					if (proxy.rayOrigins.ContainsValue(rayOrigin) && otherRayOrigin != rayOrigin)
					{
						Transform menuOrigin;
						if (proxy.menuOrigins.TryGetValue(otherRayOrigin, out menuOrigin))
						{
							if (deviceData.customMenu == null)
							{
								go = ui.InstantiateUI(prefab.gameObject, menuOrigin, false);

								var customMenu = go.GetComponent<IMenu>();
								deviceData.customMenu = customMenu;
								deviceData.menuHideFlags[customMenu] = 0;
							}
						}
					}
				});

				return go;
			}

			internal static IMainMenu SpawnMainMenu(Type type, InputDevice device, bool visible, out ActionMapInput input)
			{
				input = null;

				if (!typeof(IMainMenu).IsAssignableFrom(type))
					return null;

				var mainMenu = (IMainMenu)ObjectUtils.AddComponent(type, evr.gameObject);
				input = evr.GetModule<DeviceInputModule>().CreateActionMapInputForObject(mainMenu, device);
				evr.m_Interfaces.ConnectInterfaces(mainMenu, device);
				mainMenu.visible = visible;

				return mainMenu;
			}

			internal static IAlternateMenu SpawnAlternateMenu(Type type, InputDevice device, out ActionMapInput input)
			{
				input = null;

				if (!typeof(IAlternateMenu).IsAssignableFrom(type))
					return null;

				var alternateMenu = (IAlternateMenu)ObjectUtils.AddComponent(type, evr.gameObject);
				input = evr.GetModule<DeviceInputModule>().CreateActionMapInputForObject(alternateMenu, device);
				evr.m_Interfaces.ConnectInterfaces(alternateMenu, device);
				alternateMenu.visible = false;

				return alternateMenu;
			}

			internal static MainMenuActivator SpawnMainMenuActivator(InputDevice device)
			{
				var mainMenuActivator = ObjectUtils.Instantiate(evr.m_MainMenuActivatorPrefab.gameObject).GetComponent<MainMenuActivator>();
				evr.m_Interfaces.ConnectInterfaces(mainMenuActivator, device);

				return mainMenuActivator;
			}

			public static PinnedToolButton SpawnPinnedToolButton(InputDevice device)
			{
				var button = ObjectUtils.Instantiate(evr.m_PinnedToolButtonPrefab.gameObject).GetComponent<PinnedToolButton>();
				evr.m_Interfaces.ConnectInterfaces(button, device);

				return button;
			}

			internal static void UpdateAlternateMenuActions()
			{
				var actionsModule = evr.GetModule<ActionsModule>();
				foreach (var deviceData in evr.m_DeviceData)
				{
					var altMenu = deviceData.alternateMenu;
					if (altMenu != null)
						altMenu.menuActions = actionsModule.menuActions;
				}
			}
		}
	}
}
#endif
