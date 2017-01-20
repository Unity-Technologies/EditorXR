#if UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		[SerializeField]
		MainMenuActivator m_MainMenuActivatorPrefab;

		[SerializeField]
		PinnedToolButton m_PinnedToolButtonPrefab;

		[Flags]
		enum MenuHideFlags
		{
			Hidden = 1 << 0,
			OverActivator = 1 << 1,
			NearWorkspace = 1 << 2,
		}

		List<Type> m_MainMenuTools;

		// Local method use only -- created here to reduce garbage collection
		readonly List<IMenu> m_UpdateVisibilityMenus = new List<IMenu>();
		readonly List<DeviceData> m_ActiveDeviceData = new List<DeviceData>();

		void UpdateMenuVisibilityNearWorkspaces()
		{
			ForEachProxyDevice((deviceData) =>
			{
				m_UpdateVisibilityMenus.Clear();
				m_UpdateVisibilityMenus.AddRange(deviceData.menuHideFlags.Keys);
				for (int i = 0; i < m_UpdateVisibilityMenus.Count; i++)
				{
					var menu = m_UpdateVisibilityMenus[i];
					var menuSizes = deviceData.menuSizes;
					var menuBounds = U.Object.GetBounds(menu.menuContent);
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
					for (int j = 0; j < m_Workspaces.Count; j++)
					{
						var workspace = m_Workspaces[j];
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
									// If the device is in front of the front-panel, pointing at the front-panel, and the front-panel is not parallel to the top-panel, allow for vaild intersection
									intersection = true;
								}
								// Handle for top-panel user interactions if the user is not attempting to interact with the front-face of a standard workspace
								else if (Vector3.Dot(deviceForwardVector, workspaceTopFaceForwardVector) < -0.35f  // verify that the user is pointing at the top of the workspace
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

		void UpdateAlternateMenuForDevice(DeviceData deviceData)
		{
			var alternateMenu = deviceData.alternateMenu;
			alternateMenu.visible = deviceData.menuHideFlags[alternateMenu] == 0 && !(deviceData.currentTool is IExclusiveMode);

			// Move the activator button to an alternate position if the alternate menu will be shown
			var mainMenuActivator = deviceData.mainMenuActivator;
			if (mainMenuActivator != null)
				mainMenuActivator.activatorButtonMoveAway = alternateMenu.visible;
		}

		void UpdateMenuVisibilities()
		{
			m_ActiveDeviceData.Clear();
			ForEachProxyDevice((deviceData) =>
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
			ForEachProxyDevice((deviceData) =>
			{
				var mainMenu = deviceData.mainMenu;
				mainMenu.visible = deviceData.menuHideFlags[mainMenu] == 0;

				var customMenu = deviceData.customMenu;
				if (customMenu != null)
					customMenu.visible = deviceData.menuHideFlags[customMenu] == 0;

				UpdateAlternateMenuForDevice(deviceData);
				UpdateRayForDevice(deviceData, deviceData.rayOrigin);
			});

			UpdatePlayerHandleMaps();
		}

		void OnMainMenuActivatorHoverStarted(Transform rayOrigin)
		{
			var deviceData = m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
			if (deviceData != null)
			{
				var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
				foreach (var menu in menus)
				{
					deviceData.menuHideFlags[menu] |= MenuHideFlags.OverActivator;
				}
			}
		}

		void OnMainMenuActivatorHoverEnded(Transform rayOrigin)
		{
			var deviceData = m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
			if (deviceData != null)
			{
				var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
				foreach (var menu in menus)
				{
					deviceData.menuHideFlags[menu] &= ~MenuHideFlags.OverActivator;
				}
			}
		}

		void UpdateAlternateMenuOnSelectionChanged(Transform rayOrigin)
		{
			SetAlternateMenuVisibility(rayOrigin, Selection.gameObjects.Length > 0);
		}

		void SetAlternateMenuVisibility(Transform rayOrigin, bool visible)
		{
			ForEachProxyDevice((deviceData) =>
			{
				var alternateMenu = deviceData.alternateMenu;
				if (alternateMenu != null)
				{
					var flags = deviceData.menuHideFlags[alternateMenu];
					deviceData.menuHideFlags[alternateMenu] = (deviceData.rayOrigin == rayOrigin) && visible ? flags & ~MenuHideFlags.Hidden : flags | MenuHideFlags.Hidden;
				}
			});
		}

		void OnMainMenuActivatorSelected(Transform rayOrigin, Transform targetRayOrigin)
		{
			var deviceData = m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
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

		private GameObject InstantiateMenuUI(Transform rayOrigin, IMenu prefab)
		{
			GameObject go = null;
			ForEachProxyDevice((deviceData) =>
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
							go = InstantiateUI(prefab.gameObject);

							go.transform.SetParent(menuOrigin);
							go.transform.localPosition = Vector3.zero;
							go.transform.localRotation = Quaternion.identity;

							var customMenu = go.GetComponent<IMenu>();
							deviceData.customMenu = customMenu;
							deviceData.menuHideFlags[customMenu] = 0;
						}
					}
				}
			});

			return go;
		}

		IMainMenu SpawnMainMenu(Type type, InputDevice device, bool visible, out ActionMapInput input)
		{
			input = null;

			if (!typeof(IMainMenu).IsAssignableFrom(type))
				return null;

			var mainMenu = U.Object.AddComponent(type, gameObject) as IMainMenu;
			input = CreateActionMapInputForObject(mainMenu, device);
			ConnectInterfaces(mainMenu, device);
			mainMenu.visible = visible;

			return mainMenu;
		}

		IAlternateMenu SpawnAlternateMenu(Type type, InputDevice device, out ActionMapInput input)
		{
			input = null;

			if (!typeof(IAlternateMenu).IsAssignableFrom(type))
				return null;

			var alternateMenu = U.Object.AddComponent(type, gameObject) as IAlternateMenu;
			input = CreateActionMapInputForObject(alternateMenu, device);
			ConnectInterfaces(alternateMenu, device);
			alternateMenu.visible = false;

			return alternateMenu;
		}

		MainMenuActivator SpawnMainMenuActivator(InputDevice device)
		{
			var mainMenuActivator = U.Object.Instantiate(m_MainMenuActivatorPrefab.gameObject).GetComponent<MainMenuActivator>();
			ConnectInterfaces(mainMenuActivator, device);

			return mainMenuActivator;
		}

		PinnedToolButton SpawnPinnedToolButton(InputDevice device)
		{
			var button = U.Object.Instantiate(m_PinnedToolButtonPrefab.gameObject).GetComponent<PinnedToolButton>();
			ConnectInterfaces(button, device);

			return button;
		}
	}
}
#endif
