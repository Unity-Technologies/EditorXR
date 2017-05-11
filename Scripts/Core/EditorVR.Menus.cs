#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
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
				OverActivator = 1 << 1,
				NearWorkspace = 1 << 2,
			}

			readonly Dictionary<Type, ISettingsMenuProvider> m_SettingsMenuProviders = new Dictionary<Type, ISettingsMenuProvider>();
			List<Type> m_MainMenuTools;

			// Local method use only -- created here to reduce garbage collection
			readonly List<IMenu> m_UpdateVisibilityMenus = new List<IMenu>();
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

			internal void UpdateMenuVisibilityNearWorkspaces()
			{
				var workspaces = evr.GetModule<WorkspaceModule>().workspaces;
				Rays.ForEachProxyDevice(deviceData =>
				{
					m_UpdateVisibilityMenus.Clear();
					m_UpdateVisibilityMenus.AddRange(deviceData.menuHideFlags.Keys);
					for (int i = 0; i < m_UpdateVisibilityMenus.Count; i++)
					{
						var menu = m_UpdateVisibilityMenus[i];
						var menuSizes = deviceData.menuSizes;
						var menuBounds = ObjectUtils.GetBounds(menu.menuContent.transform);
						var menuBoundsSize = menuBounds.size;
						float size; // Because menus can change size, store the maximum size to avoid ping ponging visibility
						if (!menuSizes.TryGetValue(menu, out size))
						{
							size = menuBoundsSize.AveragedComponents();
							menuSizes[menu] = size;
						}

						var menuHideFlags = deviceData.menuHideFlags;
						var flags = menuHideFlags[menu];
						var currentMaxComponent = menuBoundsSize.AveragedComponents();
						if (currentMaxComponent > size && flags == 0)
						{
							size = currentMaxComponent;
							menuSizes[menu] = currentMaxComponent;
						}

						var intersection = false;
						for (int j = 0; j < workspaces.Count; j++)
						{
							var workspace = workspaces[j];
							var workspaceTransform = workspace.transform;
							var outerBounds = workspaceTransform.TransformBounds(workspace.outerBounds);
							var workspaceVerticalOrentationDot = Vector3.Dot(Vector3.up, workspaceTransform.forward);
							const float kWorkspaceVerticalOrientationDotThreshold = 0.85f;
							const float kWorkspaceVerticalOrientationDotRange = 1f - kWorkspaceVerticalOrientationDotThreshold;
							if (workspaceVerticalOrentationDot > kWorkspaceVerticalOrientationDotThreshold)
							{
								// Increase the height of the workspace if the top-panel is rotated parallel to the Y-axis
								// Extend the height of the workspace, to include potential front-panel blendshape bounds additions not accounted for in the bounds for better UX usability
								var lerpAmount = Mathf.Abs(kWorkspaceVerticalOrientationDotThreshold - workspaceVerticalOrentationDot) / kWorkspaceVerticalOrientationDotRange;
								var currentBounds = outerBounds.extents;
								const float kBoundsHeightIncrease = 0.075f;
								currentBounds.y += Mathf.Lerp(0f, kBoundsHeightIncrease, lerpAmount);
								outerBounds.extents = currentBounds;
							}

							if (flags == 0)
							{
								var extentsReduction = menu is IMainMenu ? 0.85f : 0.65f;
								outerBounds.extents -= Vector3.one * size * extentsReduction;
							}

							if (menuBounds.Intersects(outerBounds))
							{
								var standardWorkspace = workspace as Workspace;
								var rayOrigin = deviceData.rayOrigin;
								var deviceForwardVector = rayOrigin.forward;
								var workspaceTopFaceForwardVector = workspaceTransform.up;
								if (standardWorkspace)
								{
									var frontPanelTransform = standardWorkspace.frontPanel;
									var frontPanelForwardVector = frontPanelTransform.forward;
									var topPanelPosition = standardWorkspace.topPanel.position;
									var devicePositionTopPanelDot = Vector3.Dot(workspaceTopFaceForwardVector, topPanelPosition - rayOrigin.position);
									if (devicePositionTopPanelDot > -0.02f && devicePositionTopPanelDot < 0.095f // Verify that the device is within the front-panel's top & bottom boundaries
										&& Vector3.Dot(frontPanelForwardVector, workspaceTopFaceForwardVector) > -0.7f // Verify the front-panel is not parallel with the workspace top-panel
										&& Vector3.Dot(frontPanelForwardVector, frontPanelTransform.position - rayOrigin.position) > 0 // Verify that the device is in front of the front-panel
										&& Vector3.Dot(frontPanelForwardVector, deviceForwardVector) > 0.75f) // Verify that the device is pointing towards the front-panel
									{
										// If the device is in front of the front-panel, pointing at the front-panel, and the front-panel is not parallel to the top-panel, allow for valid intersection
										intersection = true;
									}

									// Handle for top-panel user interactions if the user is not attempting to interact with the front-face of a standard workspace
									else if (Vector3.Dot(deviceForwardVector, workspaceTopFaceForwardVector) < -0.35f // verify that the user is pointing at the top of the workspace
										&& Vector3.Dot(workspaceTopFaceForwardVector, topPanelPosition - rayOrigin.position) < 0) // Verify that the device is in front of the top-panel)
									{
										// Hide the menu if the device is above the top of the workspace, within the intersection range, and pointing towards the workspace
										intersection = true;
									}
								}

								// Support non-standard workspace menu hiding
								else if (Vector3.Dot(deviceForwardVector, workspaceTopFaceForwardVector) < -0.35f)
									intersection = true;

								if (intersection)
									break;
							}
						}

						menuHideFlags[menu] = intersection ? flags | MenuHideFlags.NearWorkspace : flags & ~MenuHideFlags.NearWorkspace;
					}
				});
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

			internal static void OnMainMenuActivatorHoverStarted(Transform rayOrigin)
			{
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] |= MenuHideFlags.OverActivator;
					}
				}
			}

			internal static void OnMainMenuActivatorHoverEnded(Transform rayOrigin)
			{
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] &= ~MenuHideFlags.OverActivator;
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
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
				{
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null)
					{
						var menuHideFlags = deviceData.menuHideFlags;
						menuHideFlags[mainMenu] ^= MenuHideFlags.Hidden;

						var customMenu = deviceData.customMenu;
						if (customMenu != null)
							menuHideFlags[customMenu] &= ~MenuHideFlags.Hidden;

						mainMenu.targetRayOrigin = targetRayOrigin;
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
