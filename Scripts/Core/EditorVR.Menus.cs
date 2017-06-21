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
				OverUI = 1 << 1,
				OverWorkspace = 1 << 2,
				Overridden = 1 << 3,
				HasDirectSelection = 1 << 4
			}

			const float k_MenuHideMargin = 0.8f;
			const float k_TwoHandHideDistance = 0.25f;

			readonly Dictionary<Type, ISettingsMenuProvider> m_SettingsMenuProviders = new Dictionary<Type, ISettingsMenuProvider>();
			List<Type> m_MainMenuTools;

			// Local method use only -- created here to reduce garbage collection
			readonly List<DeviceData> m_ActiveDeviceData = new List<DeviceData>();

			public Menus()
			{
				IInstantiateMenuUIMethods.instantiateMenuUI = InstantiateMenuUI;
				IIsMainMenuVisibleMethods.isMainMenuVisible = IsMainMenuVisible;
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

				var directSelection = evr.GetNestedModule<DirectSelection>();

				foreach (var deviceData in m_ActiveDeviceData)
				{
					var alternateMenu = deviceData.alternateMenu;
					var mainMenu = deviceData.mainMenu;
					var customMenu = deviceData.customMenu;
					var menuHideFlags = deviceData.menuHideFlags;

					var mainMenuVisible = mainMenu != null && (menuHideFlags[mainMenu] & MenuHideFlags.Hidden) == 0;
					var customMenuVisible = customMenu != null && (menuHideFlags[customMenu] & MenuHideFlags.Hidden) == 0 && (menuHideFlags[customMenu] & MenuHideFlags.Overridden) == 0;
					var alternateMenuVisible = alternateMenu != null && (menuHideFlags[alternateMenu] & MenuHideFlags.Hidden) == 0;

					if (alternateMenuVisible && (mainMenuVisible || customMenuVisible))
					{
						foreach (var otherDeviceData in m_ActiveDeviceData)
						{
							if (otherDeviceData == deviceData)
								continue;

							SetAlternateMenuVisibility(otherDeviceData.rayOrigin, true);
							break;
						}
					}

					if (customMenuVisible && (mainMenuVisible || alternateMenuVisible))
						menuHideFlags[customMenu] |= MenuHideFlags.Overridden;

					var hoveringWorkspace = false;
					var rayOrigin = deviceData.rayOrigin;
					var rayOriginPosition = rayOrigin.position;
					foreach (var workspace in evr.GetModule<WorkspaceModule>().workspaces)
					{
						if (workspace.outerBounds.Contains(workspace.transform.InverseTransformPoint(rayOriginPosition)))
							hoveringWorkspace = true;
					}

					var menus = deviceData.menuHideFlags.Keys.ToList();
					foreach (var menu in menus)
					{
						if (hoveringWorkspace)
							deviceData.menuHideFlags[menu] |= MenuHideFlags.OverWorkspace;
						else
							deviceData.menuHideFlags[menu] &= ~MenuHideFlags.OverWorkspace;
					}

					var heldObjects = directSelection.GetHeldObjects(rayOrigin);
					var hasDirectSelection = directSelection != null && heldObjects != null;
					if (hasDirectSelection)
					{
						foreach (var menu in menus)
						{
							deviceData.menuHideFlags[menu] |= MenuHideFlags.HasDirectSelection;
						}

						foreach (var otherDeviceData in m_ActiveDeviceData)
						{
							if (otherDeviceData == deviceData)
								continue;

							var otherRayOrigin = otherDeviceData.rayOrigin;
							if (directSelection.IsHovering(otherRayOrigin) || Vector3.Distance(otherRayOrigin.position, rayOriginPosition) < k_TwoHandHideDistance * Viewer.GetViewerScale())
							{
								var otherMenus = otherDeviceData.menuHideFlags.Keys.ToList();
								foreach (var menu in otherMenus)
								{
									otherDeviceData.menuHideFlags[menu] |= MenuHideFlags.HasDirectSelection;
								}
								break;
							}
						}
					}
				}

				// Apply state to UI visibility
				foreach (var deviceData in m_ActiveDeviceData)
				{
					var mainMenu = deviceData.mainMenu;
					mainMenu.visible = deviceData.menuHideFlags[mainMenu] == 0;

					var customMenu = deviceData.customMenu;
					if (customMenu != null)
						customMenu.visible = deviceData.menuHideFlags[customMenu] == 0;

					UpdateAlternateMenuForDevice(deviceData);
					Rays.UpdateRayForDevice(deviceData, deviceData.rayOrigin);
				}

				// Reset Temporary states
				foreach (var deviceData in m_ActiveDeviceData)
				{
					var menus = deviceData.menuHideFlags.Keys.ToList();
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] &= ~MenuHideFlags.OverUI;
						deviceData.menuHideFlags[menu] &= ~MenuHideFlags.HasDirectSelection;
					}
				}

				evr.GetModule<DeviceInputModule>().UpdatePlayerHandleMaps();
			}

			internal static void OnUIHoverStarted(GameObject go, RayEventData rayEventData)
			{
				OnHover(go, rayEventData);
			}

			internal static void OnUIHovering(GameObject go, RayEventData rayEventData)
			{
				OnHover(go, rayEventData);
			}

			internal static void OnUIHoverEnded(GameObject go, RayEventData rayEventData)
			{
				OnHover(go, rayEventData);
			}

			internal static void OnHover(GameObject go, RayEventData rayEventData)
			{
				if (go == evr.gameObject)
					return;

				var rayOrigin = rayEventData.rayOrigin;
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
				{
					if (go.transform.IsChildOf(deviceData.rayOrigin)) // Don't let UI on this hand block the menu
						return;

					var scaledPointerDistance = rayEventData.pointerCurrentRaycast.distance / Viewer.GetViewerScale();
					var isManipulator = go.GetComponentInParent<IManipulator>() != null;
					var menus = deviceData.menuHideFlags.Keys.ToList();
					foreach (var menu in menus)
					{
						// Only set if hidden--value is reset every frame
						if (!(isManipulator || scaledPointerDistance > menu.hideDistance + k_MenuHideMargin))
							deviceData.menuHideFlags[menu] |= MenuHideFlags.OverUI;
					}
				}
			}

			internal static void UpdateAlternateMenuOnSelectionChanged(Transform rayOrigin)
			{
				if (rayOrigin == null)
					return;

				SetAlternateMenuVisibility(rayOrigin, Selection.gameObjects.Length > 0);
			}

			internal static void SetAlternateMenuVisibility(Transform rayOrigin, bool visible)
			{
				Rays.ForEachProxyDevice(deviceData =>
				{
					var menuHideFlags = deviceData.menuHideFlags;
					var alternateMenu = deviceData.alternateMenu;
					if (alternateMenu != null)
					{
						var alternateMenuFlags = menuHideFlags[alternateMenu];
						menuHideFlags[alternateMenu] = (deviceData.rayOrigin == rayOrigin) && visible ? alternateMenuFlags & ~MenuHideFlags.Hidden : alternateMenuFlags | MenuHideFlags.Hidden;

						if ((menuHideFlags[alternateMenu] & MenuHideFlags.Hidden) != 0)
						{
							var customMenu = deviceData.customMenu;

							if (customMenu != null && (menuHideFlags[deviceData.mainMenu] & MenuHideFlags.Hidden) != 0)
							{
								menuHideFlags[customMenu] &= ~MenuHideFlags.Overridden;
							}
						}
					}
				});
			}

			internal static void OnMainMenuActivatorSelected(Transform rayOrigin, Transform targetRayOrigin)
			{
				foreach (var deviceData in evr.m_DeviceData)
				{
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null)
					{
						var customMenu = deviceData.customMenu;
						var alternateMenu = deviceData.alternateMenu;
						var menuHideFlags = deviceData.menuHideFlags;
						var alternateMenuVisible = alternateMenu != null && (menuHideFlags[alternateMenu] & MenuHideFlags.Hidden) == 0;

						if (deviceData.rayOrigin == rayOrigin)
						{
							menuHideFlags[mainMenu] ^= MenuHideFlags.Hidden;
							mainMenu.targetRayOrigin = targetRayOrigin;
						}
						else
						{
							menuHideFlags[mainMenu] |= MenuHideFlags.Hidden;

							var customMenuOverridden = customMenu != null && (menuHideFlags[customMenu] & MenuHideFlags.Overridden) != 0;
							// Move alternate menu if overriding custom menu
							if (customMenuOverridden && alternateMenuVisible)
							{
								foreach (var otherDeviceData in evr.m_DeviceData)
								{
									if (deviceData == otherDeviceData)
										continue;

									if (otherDeviceData.alternateMenu != null)
										SetAlternateMenuVisibility(rayOrigin, true);
								}
							}
						}

						alternateMenuVisible = alternateMenu != null && (menuHideFlags[alternateMenu] & MenuHideFlags.Hidden) == 0;
						var mainMenuVisible = (menuHideFlags[mainMenu] & MenuHideFlags.Hidden) == 0;
						if (customMenu != null && !alternateMenuVisible && !mainMenuVisible)
							menuHideFlags[customMenu] &= ~MenuHideFlags.Overridden;
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

			static bool IsMainMenuVisible(Transform rayOrigin)
			{
				foreach (var deviceData in evr.m_DeviceData)
				{
					if (deviceData.rayOrigin == rayOrigin)
						return deviceData.mainMenu.visible;
				}

				return false;
			}
		}
	}
}
#endif
