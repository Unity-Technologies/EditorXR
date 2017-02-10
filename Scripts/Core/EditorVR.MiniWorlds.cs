#if UNITY_EDITORVR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		class MiniWorlds : Nested
		{
			const float kPreviewScale = 0.1f;

			internal class MiniWorldRay
			{
				public Transform originalRayOrigin;
				public IMiniWorld miniWorld;
				public IProxy proxy;
				public Node node;
				public ActionMapInput directSelectInput;
				public IntersectionTester tester;
				public Transform[] dragObjects;

				public Vector3[] originalScales;
				public Vector3 previewScaleFactor;

				public bool wasHeld;
				public Vector3[] originalPositionOffsets;
				public Quaternion[] originalRotationOffsets;

				public bool wasContained;
			}

			public Dictionary<Transform, MiniWorldRay> rays { get { return m_Rays; } }
			readonly Dictionary<Transform, MiniWorldRay> m_Rays = new Dictionary<Transform, MiniWorldRay>();

			public List<IMiniWorld> worlds { get { return m_Worlds; } }
			readonly List<IMiniWorld> m_Worlds = new List<IMiniWorld>();

			bool m_MiniWorldIgnoreListDirty = true;

			internal MiniWorlds()
			{
				EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
			}

			~MiniWorlds()
			{
				EditorApplication.hierarchyWindowChanged -= OnHierarchyChanged;
			}

			void OnHierarchyChanged()
			{
				m_MiniWorldIgnoreListDirty = true;
			}

			/// <summary>
			/// Re-use DefaultProxyRay and strip off objects and components not needed for MiniWorldRays
			/// </summary>
			internal Transform InstantiateMiniWorldRay()
			{
				var miniWorldRay = U.Object.Instantiate(evr.m_ProxyRayPrefab.gameObject).transform;
				U.Object.Destroy(miniWorldRay.GetComponent<DefaultProxyRay>());

				var renderers = miniWorldRay.GetComponentsInChildren<Renderer>();
				foreach (var renderer in renderers)
				{
					if (!renderer.GetComponent<IntersectionTester>())
						U.Object.Destroy(renderer.gameObject);
					else
						renderer.enabled = false;
				}

				return miniWorldRay;
			}

			void UpdateMiniWorldIgnoreList()
			{
				var renderers = new List<Renderer>(evr.GetComponentsInChildren<Renderer>(true));
				var ignoreList = new List<Renderer>(renderers.Count);

				foreach (var r in renderers)
				{
					if (r.CompareTag(kVRPlayerTag))
						continue;

					if (r.gameObject.layer != LayerMask.NameToLayer("UI") && r.CompareTag(MiniWorldRenderer.kShowInMiniWorldTag))
						continue;

					ignoreList.Add(r);
				}

				foreach (var miniWorld in m_Worlds)
				{
					miniWorld.ignoreList = ignoreList;
				}
			}

			internal void UpdateMiniWorlds()
			{
				if (m_MiniWorldIgnoreListDirty)
				{
					UpdateMiniWorldIgnoreList();
					m_MiniWorldIgnoreListDirty = false;
				}

				var directSelection = evr.m_DirectSelection.objectsGrabber;

				// Update MiniWorldRays
				foreach (var ray in m_Rays)
				{
					var miniWorldRayOrigin = ray.Key;
					var miniWorldRay = ray.Value;

					if (!miniWorldRay.proxy.active)
					{
						miniWorldRay.tester.active = false;
						continue;
					}

					// Transform into reference space
					var miniWorld = miniWorldRay.miniWorld;
					var originalRayOrigin = miniWorldRay.originalRayOrigin;
					var referenceTransform = miniWorld.referenceTransform;
					miniWorldRayOrigin.position = referenceTransform.position + Vector3.Scale(miniWorld.miniWorldTransform.InverseTransformPoint(originalRayOrigin.position), miniWorld.referenceTransform.localScale);
					miniWorldRayOrigin.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorld.miniWorldTransform.rotation) * originalRayOrigin.rotation;
					miniWorldRayOrigin.localScale = Vector3.Scale(miniWorld.miniWorldTransform.localScale.Inverse(), referenceTransform.localScale);

					// Set miniWorldRayOrigin active state based on whether controller is inside corresponding MiniWorld
					var originalPointerPosition = originalRayOrigin.position + originalRayOrigin.forward * evr.m_DirectSelection.GetPointerLength(originalRayOrigin);
					var isContained = miniWorld.Contains(originalPointerPosition);
					miniWorldRay.tester.active = isContained;
					miniWorldRayOrigin.gameObject.SetActive(isContained);

					var directSelectInput = (DirectSelectInput)miniWorldRay.directSelectInput;
					var dragObjects = miniWorldRay.dragObjects;

					if (dragObjects == null)
					{
						var heldObjects = evr.m_DirectSelection.objectsGrabber.GetHeldObjects(miniWorldRayOrigin);
						if (heldObjects != null)
						{
							// Only one ray can grab an object, otherwise PlaceObject is called on each trigger release
							// This does not prevent TransformTool from doing two-handed scaling
							var otherRayHasObject = false;
							foreach (var otherRay in m_Rays.Values)
							{
								if (otherRay != miniWorldRay && otherRay.dragObjects != null)
									otherRayHasObject = true;
							}

							if (!otherRayHasObject)
							{
								miniWorldRay.dragObjects = heldObjects;
								var scales = new Vector3[heldObjects.Length];
								var dragGameObjects = new GameObject[heldObjects.Length];
								for (var i = 0; i < heldObjects.Length; i++)
								{
									var dragObject = heldObjects[i];
									scales[i] = dragObject.transform.localScale;
									dragGameObjects[i] = dragObject.gameObject;
								}

								var totalBounds = U.Object.GetBounds(dragGameObjects);
								var maxSizeComponent = totalBounds.size.MaxComponent();
								if (!Mathf.Approximately(maxSizeComponent, 0f))
									miniWorldRay.previewScaleFactor = Vector3.one * (kPreviewScale / maxSizeComponent);

								miniWorldRay.originalScales = scales;
							}
						}
					}

					// Transfer objects to and from original ray and MiniWorld ray (e.g. outside to inside mini world)
					if (directSelection != null && isContained != miniWorldRay.wasContained)
					{
						var pointerLengthDiff = evr.m_DirectSelection.GetPointerLength(miniWorldRayOrigin) - evr.m_DirectSelection.GetPointerLength(originalRayOrigin);
						var from = isContained ? originalRayOrigin : miniWorldRayOrigin;
						var to = isContained ? miniWorldRayOrigin : originalRayOrigin;
						if (isContained || miniWorldRay.dragObjects == null)
							directSelection.TransferHeldObjects(from, to, pointerLengthDiff * Vector3.forward);
					}

					// Transfer objects between MiniWorlds
					if (dragObjects == null)
					{
						if (isContained)
						{
							foreach (var kvp in m_Rays)
							{
								var otherRayOrigin = kvp.Key;
								var otherRay = kvp.Value;
								var otherObjects = otherRay.dragObjects;
								if (otherRay != miniWorldRay && !otherRay.wasContained && otherObjects != null)
								{
									dragObjects = otherObjects;
									miniWorldRay.dragObjects = otherObjects;
									miniWorldRay.originalScales = otherRay.originalScales;
									miniWorldRay.previewScaleFactor = otherRay.previewScaleFactor;

									otherRay.dragObjects = null;

									if (directSelection != null)
									{
										var heldObjects = directSelection.GetHeldObjects(otherRayOrigin);
										if (heldObjects != null)
										{
											directSelection.TransferHeldObjects(otherRayOrigin, miniWorldRayOrigin,
												Vector3.zero); // Set the new offset to zero because the object will have moved (this could be improved by taking original offset into account)
										}
									}

									break;
								}
							}
						}
					}

					if (isContained && !miniWorldRay.wasContained)
					{
						Rays.HideRay(originalRayOrigin, true);
						Rays.LockRay(originalRayOrigin, this);
					}

					if (!isContained && miniWorldRay.wasContained)
					{
						Rays.UnlockRay(originalRayOrigin, this);
						Rays.ShowRay(originalRayOrigin, true);
					}

					if (dragObjects == null)
					{
						miniWorldRay.wasContained = isContained;
						continue;
					}

					var previewScaleFactor = miniWorldRay.previewScaleFactor;
					var positionOffsets = miniWorldRay.originalPositionOffsets;
					var rotationOffsets = miniWorldRay.originalRotationOffsets;
					var originalScales = miniWorldRay.originalScales;

					if (directSelectInput.select.isHeld)
					{
						if (isContained)
						{
							// Scale the object back to its original scale when it re-enters the MiniWorld
							if (!miniWorldRay.wasContained)
							{
								for (var i = 0; i < dragObjects.Length; i++)
								{
									var dragObject = dragObjects[i];
									dragObject.localScale = originalScales[i];
									U.Math.SetTransformOffset(miniWorldRayOrigin, dragObject, positionOffsets[i], rotationOffsets[i]);
								}

								// Add the object (back) to TransformTool
								if (directSelection != null)
									directSelection.GrabObjects(miniWorldRay.node, miniWorldRayOrigin, directSelectInput, dragObjects);
							}
						}
						else
						{
							// Check for player head
							for (var i = 0; i < dragObjects.Length; i++)
							{
								var dragObject = dragObjects[i];
								if (dragObject.CompareTag(kVRPlayerTag))
								{
									if (directSelection != null)
										directSelection.DropHeldObjects(miniWorldRayOrigin);

									// Drop player at edge of MiniWorld
									miniWorldRay.dragObjects = null;
									dragObjects = null;
									break;
								}
							}

							if (dragObjects == null)
								continue;

							if (miniWorldRay.wasContained)
							{
								var containedInOtherMiniWorld = false;
								foreach (var world in m_Worlds)
								{
									if (miniWorld != world && world.Contains(originalPointerPosition))
										containedInOtherMiniWorld = true;
								}

								// Don't switch to previewing the objects we are dragging if we are still in another mini world
								if (!containedInOtherMiniWorld)
								{
									for (var i = 0; i < dragObjects.Length; i++)
									{
										var dragObject = dragObjects[i];

										// Store the original scale in case the object re-enters the MiniWorld
										originalScales[i] = dragObject.localScale;

										dragObject.localScale = Vector3.Scale(dragObject.localScale, previewScaleFactor);
									}

									// Drop from TransformTool to take control of object
									if (directSelection != null)
									{
										directSelection.DropHeldObjects(miniWorldRayOrigin, out positionOffsets, out rotationOffsets);
										miniWorldRay.originalPositionOffsets = positionOffsets;
										miniWorldRay.originalRotationOffsets = rotationOffsets;
										miniWorldRay.wasHeld = true;
									}
								}
							}

							for (var i = 0; i < dragObjects.Length; i++)
							{
								var dragObject = dragObjects[i];
								var rotation = originalRayOrigin.rotation;
								var position = originalRayOrigin.position
									+ rotation * Vector3.Scale(previewScaleFactor, positionOffsets[i]);
								U.Math.LerpTransform(dragObject, position, rotation * rotationOffsets[i]);
							}
						}
					}

					// Release the current object if the trigger is no longer held
					if (directSelectInput.select.wasJustReleased)
					{
						var rayPosition = originalRayOrigin.position;
						for (var i = 0; i < dragObjects.Length; i++)
						{
							var dragObject = dragObjects[i];

							// If the user has pulled an object out of the MiniWorld, use PlaceObject to grow it back to its original scale
							if (!isContained)
							{
								if (evr.m_Viewer.IsOverShoulder(originalRayOrigin))
								{
									evr.m_ObjectModule.DeleteSceneObject(dragObject.gameObject);
								}
								else
								{
									dragObject.localScale = originalScales[i];
									var rotation = originalRayOrigin.rotation;
									dragObject.position = rayPosition + rotation * positionOffsets[i];
									dragObject.rotation = rotation * rotationOffsets[i];
								}
							}
						}

						miniWorldRay.dragObjects = null;
						miniWorldRay.wasHeld = false;
					}

					miniWorldRay.wasContained = isContained;
				}
			}

			internal void OnWorkspaceCreated(IWorkspace workspace)
			{
				// MiniWorld is a special case that we handle due to all of the mini world interactions
				var miniWorldWorkspace = workspace as MiniWorldWorkspace;
				if (!miniWorldWorkspace)
					return;

				var miniWorld = miniWorldWorkspace.miniWorld;
				m_Worlds.Add(miniWorld);

				evr.m_Rays.ForEachProxyDevice((deviceData) =>
				{
					var miniWorldRayOrigin = InstantiateMiniWorldRay();
					miniWorldRayOrigin.parent = workspace.transform;

#if ENABLE_MINIWORLD_RAY_SELECTION
				// Use the mini world ray origin instead of the original ray origin
				m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, deviceData.uiInput, miniWorldRayOrigin, (source) =>
				{
					if (!IsRayActive(source.rayOrigin))
						return false;

					if (source.hoveredObject)
						return !m_Workspaces.Any(w => source.hoveredObject.transform.IsChildOf(w.transform));

					return true;
				});
#endif

					var tester = miniWorldRayOrigin.GetComponentInChildren<IntersectionTester>();
					tester.active = false;

					m_Rays[miniWorldRayOrigin] = new MiniWorlds.MiniWorldRay
					{
						originalRayOrigin = deviceData.rayOrigin,
						miniWorld = miniWorld,
						proxy = deviceData.proxy,
						node = deviceData.node,
						directSelectInput = deviceData.directSelectInput,
						tester = tester
					};

					evr.m_IntersectionModule.AddTester(tester);
				}, false);
			}

			internal void OnWorkspaceDestroyed(IWorkspace workspace)
			{
				var miniWorldWorkspace = workspace as MiniWorldWorkspace;
				if (miniWorldWorkspace != null)
				{
					var miniWorld = miniWorldWorkspace.miniWorld;

					//Clean up MiniWorldRays
					m_Worlds.Remove(miniWorld);
					var miniWorldRaysCopy = new Dictionary<Transform, MiniWorlds.MiniWorldRay>(m_Rays);
					foreach (var ray in miniWorldRaysCopy)
					{
						var miniWorldRay = ray.Value;
						if (miniWorldRay.miniWorld == miniWorld)
						{
							var rayOrigin = ray.Key;
#if ENABLE_MINIWORLD_RAY_SELECTION
						m_InputModule.RemoveRaycastSource(rayOrigin);
#endif
							m_Rays.Remove(rayOrigin);
						}
					}
				}

			}
		}
	}
}
#endif
