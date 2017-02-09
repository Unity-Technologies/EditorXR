#if UNITY_EDITORVR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Core;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		class Interfaces : Nested
		{
			const byte kMinStencilRef = 2;

			readonly HashSet<object> m_ConnectedInterfaces = new HashSet<object>();

			byte stencilRef
			{
				get { return m_StencilRef; }
				set
				{
					m_StencilRef = (byte)Mathf.Clamp(value, kMinStencilRef, byte.MaxValue);

					// Wrap
					if (m_StencilRef == byte.MaxValue)
						m_StencilRef = kMinStencilRef;
				}
			}

			byte m_StencilRef = kMinStencilRef;

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

				if (rayOrigin)
				{
					var ray = obj as IUsesRayOrigin;
					if (ray != null)
						ray.rayOrigin = rayOrigin;

					var usesProxy = obj as IUsesProxyType;
					if (usesProxy != null)
					{
						var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
						if (deviceData != null)
							usesProxy.proxyType = deviceData.proxy.GetType();
					}

					var menuOrigins = obj as IUsesMenuOrigins;
					if (menuOrigins != null)
					{
						Transform mainMenuOrigin;
						var proxy = evr.m_Rays.GetProxyForRayOrigin(rayOrigin);
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
					locomotion.viewerPivot = VRView.viewerPivot;

				var instantiateUI = obj as IInstantiateUI;
				if (instantiateUI != null)
					instantiateUI.instantiateUI = evr.InstantiateUI;

				var createWorkspace = obj as ICreateWorkspace;
				if (createWorkspace != null)
					createWorkspace.createWorkspace = evr.CreateWorkspace;

				var instantiateMenuUI = obj as IInstantiateMenuUI;
				if (instantiateMenuUI != null)
					instantiateMenuUI.instantiateMenuUI = evr.m_Menus.InstantiateMenuUI;

				var raycaster = obj as IUsesRaycastResults;
				if (raycaster != null)
					raycaster.getFirstGameObject = evr.m_Rays.GetFirstGameObject;

				var highlight = obj as ISetHighlight;
				if (highlight != null)
					highlight.setHighlight = evr.m_HighlightModule.SetHighlight;

				var placeObjects = obj as IPlaceObject;
				if (placeObjects != null)
					placeObjects.placeObject = evr.m_ObjectModule.PlaceObject;

				var locking = obj as IUsesGameObjectLocking;
				if (locking != null)
				{
					locking.setLocked = evr.m_LockModule.SetLocked;
					locking.isLocked = evr.m_LockModule.IsLocked;
				}

				var positionPreview = obj as IGetPreviewOrigin;
				if (positionPreview != null)
					positionPreview.getPreviewOriginForRayOrigin = evr.m_Rays.GetPreviewOriginForRayOrigin;

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
							sectionName = ActionMenuItemAttribute.kDefaultActionSectionName,
							priority = int.MaxValue,
							action = action,
						};
						evr.m_ActionsModule.menuActions.Add(actionMenuData);
					}
					evr.m_Menus.UpdateAlternateMenuActions();
				}

				var directSelection = obj as IUsesDirectSelection;
				if (directSelection != null)
					directSelection.getDirectSelection = evr.m_DirectSelection.GetDirectSelection;

				var grabObjects = obj as IGrabObjects;
				if (grabObjects != null)
				{
					grabObjects.canGrabObject = evr.m_DirectSelection.CanGrabObject;
					grabObjects.objectGrabbed += evr.m_DirectSelection.OnObjectGrabbed;
					grabObjects.objectsDropped += evr.m_DirectSelection.OnObjectsDropped;
				}

				var spatialHash = obj as IUsesSpatialHash;
				if (spatialHash != null)
				{
					spatialHash.addToSpatialHash = evr.m_SpatialHashModule.AddObject;
					spatialHash.removeFromSpatialHash = evr.m_SpatialHashModule.RemoveObject;
				}

				var deleteSceneObjects = obj as IDeleteSceneObject;
				if (deleteSceneObjects != null)
					deleteSceneObjects.deleteSceneObject = evr.m_ObjectModule.DeleteSceneObject;

				var usesViewerBody = obj as IUsesViewerBody;
				if (usesViewerBody != null)
					usesViewerBody.isOverShoulder = evr.IsOverShoulder;

				var mainMenu = obj as IMainMenu;
				if (mainMenu != null)
				{
					mainMenu.menuTools = evr.m_Menus.mainMenuTools;
					mainMenu.menuWorkspaces = evr.m_AllWorkspaceTypes.ToList();
					mainMenu.isToolActive = evr.IsToolActive;
				}

				var alternateMenu = obj as IAlternateMenu;
				if (alternateMenu != null)
					alternateMenu.menuActions = evr.m_ActionsModule.menuActions;

				var usesProjectFolderData = obj as IUsesProjectFolderData;
				if (usesProjectFolderData != null)
					evr.m_ProjectFolderModule.AddConsumer(usesProjectFolderData);

				var usesHierarchyData = obj as IUsesHierarchyData;
				if (usesHierarchyData != null)
					evr.m_HierarchyModule.AddConsumer(usesHierarchyData);

				var filterUI = obj as IFilterUI;
				if (filterUI != null)
					evr.m_ProjectFolderModule.AddConsumer(filterUI);

				// Tracked Object action maps shouldn't block each other so we share an instance
				var trackedObjectMap = obj as ITrackedObjectActionMap;
				if (trackedObjectMap != null)
					trackedObjectMap.trackedObjectInput = evr.m_DeviceInputModule.trackedObjectInput;

				var selectTool = obj as ISelectTool;
				if (selectTool != null)
					selectTool.selectTool = evr.SelectTool;

				var usesViewerPivot = obj as IUsesViewerPivot;
				if (usesViewerPivot != null)
					usesViewerPivot.viewerPivot = U.Camera.GetViewerPivot();

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
					selectObject.getSelectionCandidate = evr.m_SelectionModule.GetSelectionCandidate;
					selectObject.selectObject = evr.m_SelectionModule.SelectObject;
				}

				var manipulatorVisiblity = obj as IManipulatorVisibility;
				if (manipulatorVisiblity != null)
					evr.m_ManipulatorVisibilities.Add(manipulatorVisiblity);

				var setManipulatorsVisible = obj as ISetManipulatorsVisible;
				if (setManipulatorsVisible != null)
					setManipulatorsVisible.setManipulatorsVisible = evr.SetManipulatorsVisible;

				var requestStencilRef = obj as IRequestStencilRef;
				if (requestStencilRef != null)
					requestStencilRef.requestStencilRef = RequestStencilRef;

				// Internal interfaces
				var forEachRayOrigin = obj as IForEachRayOrigin;
				if (forEachRayOrigin != null && IsSameAssembly<IForEachRayOrigin>(obj))
					forEachRayOrigin.forEachRayOrigin = evr.m_Rays.ForEachRayOrigin;
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
					grabObjects.objectGrabbed -= evr.m_DirectSelection.OnObjectGrabbed;
					grabObjects.objectsDropped -= evr.m_DirectSelection.OnObjectsDropped;
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
					evr.m_ManipulatorVisibilities.Remove(manipulatorVisiblity);
			}

			byte RequestStencilRef()
			{
				return stencilRef++;
			}
		}
	}
}
#endif
