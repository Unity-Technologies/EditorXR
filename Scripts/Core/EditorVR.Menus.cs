#if !UNITY_EDITORVR
#pragma warning disable 67, 414, 649
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	internal partial class EditorVR : MonoBehaviour
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

#if UNITY_EDITORVR
		void UpdateMenuVisibilityNearWorkspaces()
		{
			ForEachRayOrigin((proxy, pair, device, deviceData) =>
			{
				m_UpdateVisibilityMenus.Clear();
				m_UpdateVisibilityMenus.AddRange(deviceData.menuHideFlags.Keys);
				for (int i = 0; i < m_UpdateVisibilityMenus.Count; i++)
				{
					var menu = m_UpdateVisibilityMenus[i];
					// AE 12/7/16 - Disabling main menu hiding near workspaces for now because it confuses people; Needs improvement
					if (menu is IMainMenu)
						continue;

					var menuSizes = deviceData.menuSizes;
					var menuBounds = U.Object.GetBounds(menu.menuContent);
					var menuBoundsSize = menuBounds.size;

					// Because menus can change size, store the maximum size to avoid ping ponging visibility
					float maxComponent;
					if (!menuSizes.TryGetValue(menu, out maxComponent))
					{
						maxComponent = menuBoundsSize.MaxComponent();
						menuSizes[menu] = maxComponent;
					}

					var menuHideFlags = deviceData.menuHideFlags;
					var flags = menuHideFlags[menu];
					var currentMaxComponent = menuBoundsSize.MaxComponent();
					if (currentMaxComponent > maxComponent && flags == 0)
					{
						maxComponent = currentMaxComponent;
						menuSizes[menu] = currentMaxComponent;
					}

					var intersection = false;

					for (int j = 0; i < m_Workspaces.Count; i++)
					{
						var workspace = m_Workspaces[j];
						var outerBounds = workspace.transform.TransformBounds(workspace.outerBounds);
						if (flags == 0)
							outerBounds.extents -= Vector3.one * maxComponent;

						if (menuBounds.Intersects(outerBounds))
						{
							intersection = true;
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
			ForEachRayOrigin((proxy, pair, device, deviceData) =>
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
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				var mainMenu = deviceData.mainMenu;
				mainMenu.visible = deviceData.menuHideFlags[mainMenu] == 0;

				var customMenu = deviceData.customMenu;
				if (customMenu != null)
					customMenu.visible = deviceData.menuHideFlags[customMenu] == 0;

				UpdateAlternateMenuForDevice(deviceData);
				UpdateRayForDevice(deviceData, rayOriginPair.Value);
			});

			UpdatePlayerHandleMaps();
		}

		void OnMainMenuActivatorHoverStarted(Transform rayOrigin)
		{
			ForEachRayOrigin((p, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] |= MenuHideFlags.OverActivator;
					}
				}
			});
		}

		void OnMainMenuActivatorHoverEnded(Transform rayOrigin)
		{
			ForEachRayOrigin((p, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] &= ~MenuHideFlags.OverActivator;
					}
				}
			});
		}

		void UpdateAlternateMenuOnSelectionChanged(Transform rayOrigin)
		{
			SetAlternateMenuVisibility(rayOrigin, Selection.gameObjects.Length > 0);
		}

		void SetAlternateMenuVisibility(Transform rayOrigin, bool visible)
		{
			ForEachRayOrigin((proxy, rayOriginPair, rayOriginDevice, deviceData) =>
			{
				var alternateMenu = deviceData.alternateMenu;
				if (alternateMenu != null)
				{
					var flags = deviceData.menuHideFlags[alternateMenu];
					deviceData.menuHideFlags[alternateMenu] = (rayOriginPair.Value == rayOrigin) && visible ? flags & ~MenuHideFlags.Hidden : flags | MenuHideFlags.Hidden;
				}
			});
		}

		void OnMainMenuActivatorSelected(Transform rayOrigin, Transform targetRayOrigin)
		{
			ForEachRayOrigin((proxy, rayOriginPair, rayOriginDevice, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
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
			});
		}

		private GameObject InstantiateMenuUI(Transform rayOrigin, IMenu prefab)
		{
			GameObject go = null;
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (proxy.rayOrigins.ContainsValue(rayOrigin) && rayOriginPair.Value != rayOrigin)
				{
					var otherRayOrigin = rayOriginPair.Value;
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

#endif
	}
}
