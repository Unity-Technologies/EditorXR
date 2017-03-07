#if UNITY_EDITORVR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Core;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		class Interfaces : Nested
		{
			const byte k_MinStencilRef = 2;

			readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

			byte stencilRef
			{
				get { return m_StencilRef; }
				set
				{
					m_StencilRef = (byte)Mathf.Clamp(value, k_MinStencilRef, byte.MaxValue);

					// Wrap
					if (m_StencilRef == byte.MaxValue)
						m_StencilRef = k_MinStencilRef;
				}
			}

			byte m_StencilRef = k_MinStencilRef;

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

				var evrRays = evr.m_Rays;
				var evrWorkspaceModule = evr.m_WorkspaceModule;
				var evrMenus = evr.m_Menus;
				var evrHighlightModule = evr.m_HighlightModule;
				var evrSceneObjectModule = evr.m_SceneObjectModule;
				var evrLockModule = evr.m_LockModule;
				var evrActionsModule = evr.m_ActionsModule;
				var evrDirectSelection = evr.m_DirectSelection;
				var evrSpatialHashModule = evr.m_SpatialHashModule;
				var evrViewer = evr.m_Viewer;
				var evrTools = evr.m_Tools;
				var evrProjectFolderModule = evr.m_ProjectFolderModule;
				var evrHierarchyModule = evr.m_HierarchyModule;
				var evrDeviceInputModule = evr.m_DeviceInputModule;
				var evrSelectionModule = evr.m_SelectionModule;
				var evrUI = evr.m_UI;
				var evrDeviceData = evr.m_DeviceData;
				var tooltipModule = evr.m_TooltipModule;

				if (rayOrigin)
				{
					var ray = obj as IUsesRayOrigin;
					if (ray != null)
						ray.rayOrigin = rayOrigin;

					var deviceData = evrDeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);

					var handedRay = obj as IUsesNode;
					if (handedRay != null && deviceData != null)
						handedRay.node = deviceData.node;

					var usesProxy = obj as IUsesProxyType;
					if (usesProxy != null && deviceData != null)
						usesProxy.proxyType = deviceData.proxy.GetType();

					var menuOrigins = obj as IUsesMenuOrigins;
					if (menuOrigins != null)
					{
						Transform mainMenuOrigin;
						var proxy = evrRays.GetProxyForRayOrigin(rayOrigin);
						if (proxy != null && proxy.menuOrigins.TryGetValue(rayOrigin, out mainMenuOrigin))
						{
							menuOrigins.menuOrigin = mainMenuOrigin;
							Transform alternateMenuOrigin;
							if (proxy.alternateMenuOrigins.TryGetValue(rayOrigin, out alternateMenuOrigin))
								menuOrigins.alternateMenuOrigin = alternateMenuOrigin;
						}
					}
				}

				// Specific proxy ray setting
				var customRay = obj as ICustomRay;
				if (customRay != null)
				{
					customRay.showDefaultRay = Rays.ShowRay;
					customRay.hideDefaultRay = Rays.HideRay;
				}

				var lockableRay = obj as IUsesRayLocking;
				if (lockableRay != null)
				{
					lockableRay.lockRay = Rays.LockRay;
					lockableRay.unlockRay = Rays.UnlockRay;
				}

				var locomotion = obj as ILocomotor;
				if (locomotion != null)
					locomotion.cameraRig = VRView.cameraRig;

				var instantiateUI = obj as IInstantiateUI;
				if (instantiateUI != null)
					instantiateUI.instantiateUI = evrUI.InstantiateUI;

				var createWorkspace = obj as ICreateWorkspace;
				if (createWorkspace != null)
					createWorkspace.createWorkspace = evrWorkspaceModule.CreateWorkspace;

				var instantiateMenuUI = obj as IInstantiateMenuUI;
				if (instantiateMenuUI != null)
					instantiateMenuUI.instantiateMenuUI = evrMenus.InstantiateMenuUI;

				var raycaster = obj as IUsesRaycastResults;
				if (raycaster != null)
					raycaster.getFirstGameObject = evrRays.GetFirstGameObject;

				var highlight = obj as ISetHighlight;
				if (highlight != null)
					highlight.setHighlight = evrHighlightModule.SetHighlight;

				var placeObjects = obj as IPlaceObject;
				if (placeObjects != null)
					placeObjects.placeObject = evrSceneObjectModule.PlaceSceneObject;

				var locking = obj as IUsesGameObjectLocking;
				if (locking != null)
				{
					locking.setLocked = evrLockModule.SetLocked;
					locking.isLocked = evrLockModule.IsLocked;
				}

				var positionPreview = obj as IGetPreviewOrigin;
				if (positionPreview != null)
					positionPreview.getPreviewOriginForRayOrigin = evrRays.GetPreviewOriginForRayOrigin;

				var selectionChanged = obj as ISelectionChanged;
				if (selectionChanged != null)
					evr.m_SelectionChanged += selectionChanged.OnSelectionChanged;

				var toolActions = obj as IActions;
				if (toolActions != null)
				{
					var actions = toolActions.actions;
					foreach (var action in actions)
					{
						var actionMenuData = new ActionMenuData()
						{
							name = action.GetType().Name,
							sectionName = ActionMenuItemAttribute.DefaultActionSectionName,
							priority = int.MaxValue,
							action = action,
						};
						evrActionsModule.menuActions.Add(actionMenuData);
					}
					evrMenus.UpdateAlternateMenuActions();
				}

				var directSelection = obj as IUsesDirectSelection;
				if (directSelection != null)
					directSelection.getDirectSelection = evrDirectSelection.GetDirectSelection;

				var grabObjects = obj as IGrabObjects;
				if (grabObjects != null)
				{
					grabObjects.canGrabObject = evrDirectSelection.CanGrabObject;
					grabObjects.objectGrabbed += evrDirectSelection.OnObjectGrabbed;
					grabObjects.objectsDropped += evrDirectSelection.OnObjectsDropped;
				}

				var spatialHash = obj as IUsesSpatialHash;
				if (spatialHash != null)
				{
					spatialHash.addToSpatialHash = evrSpatialHashModule.AddObject;
					spatialHash.removeFromSpatialHash = evrSpatialHashModule.RemoveObject;
				}

				var deleteSceneObjects = obj as IDeleteSceneObject;
				if (deleteSceneObjects != null)
					deleteSceneObjects.deleteSceneObject = evrSceneObjectModule.DeleteSceneObject;

				var usesViewerBody = obj as IUsesViewerBody;
				if (usesViewerBody != null)
					usesViewerBody.isOverShoulder = evrViewer.IsOverShoulder;

				var mainMenu = obj as IMainMenu;
				if (mainMenu != null)
				{
					mainMenu.menuTools = evrMenus.mainMenuTools;
					mainMenu.menuWorkspaces = WorkspaceModule.workspaceTypes;
					mainMenu.isToolActive = evrTools.IsToolActive;
				}

				var alternateMenu = obj as IAlternateMenu;
				if (alternateMenu != null)
					alternateMenu.menuActions = evrActionsModule.menuActions;

				var usesProjectFolderData = obj as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
					evrProjectFolderModule.AddConsumer(usesProjectFolderData);

				var usesHierarchyData = obj as IUsesHierarchyData;
				if (usesHierarchyData != null)
					evrHierarchyModule.AddConsumer(usesHierarchyData);

				var filterUI = obj as IFilterUI;
				if (filterUI != null)
					evrProjectFolderModule.AddConsumer(filterUI);

				// Tracked Object action maps shouldn't block each other so we share an instance
				var trackedObjectMap = obj as ITrackedObjectActionMap;
				if (trackedObjectMap != null)
					trackedObjectMap.trackedObjectInput = evrDeviceInputModule.trackedObjectInput;

				var selectTool = obj as ISelectTool;
				if (selectTool != null)
					selectTool.selectTool = evrTools.SelectTool;

				var usesCameraRig = obj as IUsesCameraRig;
				if (usesCameraRig != null)
					usesCameraRig.cameraRig = CameraUtils.GetCameraRig();

				var usesStencilRef = obj as IUsesStencilRef;
				if (usesStencilRef != null)
				{
					byte? stencilRef = null;

					var mb = obj as MonoBehaviour;
					if (mb)
					{
						var parent = mb.transform.parent;
						if (parent)
						{
							// For workspaces and tools, it's likely that the stencil ref should be shared internally
							var parentStencilRef = parent.GetComponentInParent<IUsesStencilRef>();
							if (parentStencilRef != null)
								stencilRef = parentStencilRef.stencilRef;
						}
					}

					usesStencilRef.stencilRef = stencilRef ?? RequestStencilRef();
				}

				var selectObject = obj as ISelectObject;
				if (selectObject != null)
				{
					selectObject.getSelectionCandidate = evrSelectionModule.GetSelectionCandidate;
					selectObject.selectObject = evrSelectionModule.SelectObject;
				}

				var manipulatorVisiblity = obj as IManipulatorVisibility;
				if (manipulatorVisiblity != null)
					evrUI.manipulatorVisibilities.Add(manipulatorVisiblity);

				var setManipulatorsVisible = obj as ISetManipulatorsVisible;
				if (setManipulatorsVisible != null)
					setManipulatorsVisible.setManipulatorsVisible = evrUI.SetManipulatorsVisible;

				var requestStencilRef = obj as IRequestStencilRef;
				if (requestStencilRef != null)
					requestStencilRef.requestStencilRef = RequestStencilRef;

				var moveCameraRig = obj as IMoveCameraRig;
				if (moveCameraRig != null)
					moveCameraRig.moveCameraRig = Viewer.MoveCameraRig;

				var usesViewerScale = obj as IUsesViewerScale;
				if (usesViewerScale != null)
					usesViewerScale.getViewerScale = Viewer.GetViewerScale;

				var usesTooltip = obj as ISetTooltipVisibility;
				if (usesTooltip != null)
				{
					usesTooltip.showTooltip = tooltipModule.ShowTooltip;
					usesTooltip.hideTooltip = tooltipModule.HideTooltip;
				}

				var linkedObject = obj as ILinkedObject;
				if (linkedObject != null)
				{
					var type = obj.GetType();
					var linkedObjects = evrTools.linkedObjects;
					List<ILinkedObject> linkedObjectList;
					if (!linkedObjects.TryGetValue(type, out linkedObjectList))
					{
						linkedObjectList = new List<ILinkedObject>();
						linkedObjects[type] = linkedObjectList;
					}

					linkedObjectList.Add(linkedObject);
					linkedObject.linkedObjects = linkedObjectList;
					linkedObject.isSharedUpdater = IsSharedUpdater;
				}

				// Internal interfaces
				var forEachRayOrigin = obj as IForEachRayOrigin;
				if (forEachRayOrigin != null && IsSameAssembly<IForEachRayOrigin>(obj))
					forEachRayOrigin.forEachRayOrigin = evrRays.ForEachRayOrigin;
			}

			bool IsSharedUpdater(ILinkedObject linkedObject)
			{
				var type = linkedObject.GetType();
				return evr.m_Tools.linkedObjects[type].IndexOf(linkedObject) == 0;
			}

			static bool IsSameAssembly<T>(object obj)
			{
				// Until we move EditorVR into it's own assembly, this is a way to enforce 'internal' on interfaces
				var objType = obj.GetType();
				return objType.Assembly == typeof(T).Assembly;
			}

			internal void DisconnectInterfaces(object obj)
			{
				m_ConnectedInterfaces.Remove(obj);

				var selectionChanged = obj as ISelectionChanged;
				if (selectionChanged != null)
					evr.m_SelectionChanged -= selectionChanged.OnSelectionChanged;

				var toolActions = obj as IActions;
				if (toolActions != null)
				{
					evr.m_ActionsModule.RemoveActions(toolActions.actions);
					evr.m_Menus.UpdateAlternateMenuActions();
				}

				var grabObjects = obj as IGrabObjects;
				if (grabObjects != null)
				{
					var directSelection = evr.m_DirectSelection;
					grabObjects.objectGrabbed -= directSelection.OnObjectGrabbed;
					grabObjects.objectsDropped -= directSelection.OnObjectsDropped;
				}

				var usesProjectFolderData = obj as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
					evr.m_ProjectFolderModule.RemoveConsumer(usesProjectFolderData);

				var usesHierarchy = obj as IUsesHierarchyData;
				if (usesHierarchy != null)
					evr.m_HierarchyModule.RemoveConsumer(usesHierarchy);

				var filterUI = obj as IFilterUI;
				if (filterUI != null)
					evr.m_ProjectFolderModule.RemoveConsumer(filterUI);

				var manipulatorVisiblity = obj as IManipulatorVisibility;
				if (manipulatorVisiblity != null)
					evr.m_UI.manipulatorVisibilities.Remove(manipulatorVisiblity);
			}

			byte RequestStencilRef()
			{
				return stencilRef++;
			}
		}
	}
}

#endif
