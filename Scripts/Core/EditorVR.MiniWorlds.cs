#if UNITY_EDITOR && UNITY_EDITORVR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class MiniWorlds : Nested, ILateBindInterfaceMethods<DirectSelection>, IPlaceSceneObject, IUsesViewerScale
		{
			internal class MiniWorldRay
			{
				public Transform originalRayOrigin { get; private set; }
				public IMiniWorld miniWorld { get; private set; }
				public IProxy proxy { get; private set; }
				public Node node { get; private set; }
				public ActionMapInput directSelectInput { get; private set; }
				public IntersectionTester tester { get; private set; }

				public bool dragStartedOutside { get; set; }
				public bool isContained { get; set; }

				public bool hasPreview
				{
					get { return m_Previews.Count > 0; }
				}

				readonly List<Preview> m_Previews = new List<Preview>();

				class Preview
				{
					Transform m_Transform;
					Vector3 m_PositionOffset;
					Quaternion m_RotationOffset;
					Vector3 m_OriginalScale;

					public Preview(Transform transform, Transform parent, float scaleFactor)
					{
						m_Transform = transform;
						m_OriginalScale = transform.localScale;
						transform.localScale *= scaleFactor;
						MathUtilsExt.GetTransformOffset(parent, transform, out m_PositionOffset, out m_RotationOffset);
						m_PositionOffset *= scaleFactor;
					}

					public void Update(Transform parent)
					{
						MathUtilsExt.SetTransformOffset(parent, m_Transform, m_PositionOffset, m_RotationOffset);
					}

					public void ResetScale()
					{
						m_Transform.localScale = m_OriginalScale;
					}

					public void PlaceObject(IPlaceSceneObject placer)
					{
						placer.PlaceSceneObject(m_Transform, m_OriginalScale);
					}
				}

				public MiniWorldRay(Transform originalRayOrigin, IMiniWorld miniWorld, IProxy proxy, Node node, ActionMapInput directSelectInput, IntersectionTester tester)
				{
					this.originalRayOrigin = originalRayOrigin;
					this.miniWorld = miniWorld;
					this.proxy = proxy;
					this.node = node;
					this.directSelectInput = directSelectInput;
					this.tester = tester;
				}

				public void EnterPreviewMode(HashSet<Transform> heldObjects, Transform rayOrigin, float scaleFactor)
				{
					foreach (var heldObject in heldObjects)
					{
						m_Previews.Add(new Preview(heldObject, rayOrigin, scaleFactor));
					}
				}

				public void ExitPreviewMode()
				{
					foreach (var preview in m_Previews)
					{
						preview.ResetScale();
					}
					m_Previews.Clear();
				}

				public void DropPreviewObjects(IPlaceSceneObject placer)
				{
					foreach (var preview in m_Previews)
					{
						preview.PlaceObject(placer);
					}
					m_Previews.Clear();
				}

				public void UpdatePreview()
				{
					foreach (var preview in m_Previews)
					{
						preview.Update(originalRayOrigin);
					}
				}
			}

			public Dictionary<Transform, MiniWorldRay> rays { get { return m_Rays; } }
			readonly Dictionary<Transform, MiniWorldRay> m_Rays = new Dictionary<Transform, MiniWorldRay>();
			readonly Dictionary<Transform, bool> m_RayWasContained = new Dictionary<Transform, bool>();

			public List<IMiniWorld> worlds { get { return m_Worlds; } }
			readonly List<IMiniWorld> m_Worlds = new List<IMiniWorld>();

			bool m_MiniWorldIgnoreListDirty = true;

			public MiniWorlds()
			{
				EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
			}

			internal override void OnDestroy()
			{
				base.OnDestroy();
				EditorApplication.hierarchyWindowChanged -= OnHierarchyChanged;
			}

			void OnHierarchyChanged()
			{
				m_MiniWorldIgnoreListDirty = true;
			}

			/// <summary>
			/// Re-use DefaultProxyRay and strip off objects and components not needed for MiniWorldRays
			/// </summary>
			static Transform InstantiateMiniWorldRay()
			{
				var miniWorldRay = ObjectUtils.Instantiate(evr.m_ProxyRayPrefab.gameObject).transform;
				ObjectUtils.Destroy(miniWorldRay.GetComponent<DefaultProxyRay>());

				var renderers = miniWorldRay.GetComponentsInChildren<Renderer>();
				foreach (var renderer in renderers)
				{
					if (!renderer.GetComponent<IntersectionTester>())
						ObjectUtils.Destroy(renderer.gameObject);
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
					if (r.CompareTag(k_VRPlayerTag))
						continue;

					if (r.gameObject.layer != LayerMask.NameToLayer("UI") && r.CompareTag(MiniWorldRenderer.ShowInMiniWorldTag))
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

				var directSelection = evr.GetNestedModule<DirectSelection>();

				// Update MiniWorldRays
				foreach (var ray in m_Rays)
				{
					var miniWorldRayOrigin = ray.Key;
					var miniWorldRay = ray.Value;
					var node = miniWorldRay.node;

					if (!miniWorldRay.proxy.active)
					{
						miniWorldRay.tester.active = false;
						continue;
					}

					var miniWorld = miniWorldRay.miniWorld;
					var inverseScale = miniWorld.miniWorldTransform.lossyScale.Inverse();

					if (float.IsInfinity(inverseScale.x) || float.IsNaN(inverseScale.x)) // Extreme scales cause transform errors
						continue;

					// Transform into reference space
					var originalRayOrigin = miniWorldRay.originalRayOrigin;
					var referenceTransform = miniWorld.referenceTransform;
					var miniWorldTransform = miniWorld.miniWorldTransform;
					miniWorldRayOrigin.position = referenceTransform.TransformPoint(miniWorldTransform.InverseTransformPoint(originalRayOrigin.position));
					miniWorldRayOrigin.rotation = referenceTransform.rotation * Quaternion.Inverse(miniWorldTransform.rotation) * originalRayOrigin.rotation;
					miniWorldRayOrigin.localScale = Vector3.Scale(inverseScale, referenceTransform.localScale);

					// Set miniWorldRayOrigin active state based on whether controller is inside corresponding MiniWorld
					var originalPointerPosition = originalRayOrigin.position + originalRayOrigin.forward * DirectSelection.GetPointerLength(originalRayOrigin);
					var isContained = miniWorld.Contains(originalPointerPosition);
					miniWorldRay.tester.active = isContained;
					miniWorldRayOrigin.gameObject.SetActive(isContained);

					var miniWorldRayObjects = directSelection.GetHeldObjects(miniWorldRayOrigin);
					var originalRayObjects = directSelection.GetHeldObjects(originalRayOrigin);

					var hasPreview = miniWorldRay.hasPreview;
					if (miniWorldRayObjects == null && originalRayObjects == null && !hasPreview)
					{
						miniWorldRay.isContained = isContained;
						continue;
					}

					var wasContained = miniWorldRay.isContained;
					if (isContained != wasContained)
					{
						// Early out if we grabbed a real-world object that started inside a mini world
						if (!isContained && miniWorldRayObjects == null)
						{
							miniWorldRay.isContained = false;
							continue;
						}

						// Transfer objects to and from original ray and MiniWorld ray (e.g. outside to inside mini world)
						var from = isContained ? originalRayOrigin : miniWorldRayOrigin;
						var to = isContained ? miniWorldRayOrigin : originalRayOrigin;

						if (miniWorldRayObjects != null && !isContained)
						{
							// Try to transfer objects between MiniWorlds
							foreach (var kvp in m_Rays)
							{
								var otherRayOrigin = kvp.Key;
								var otherRay = kvp.Value;
								if (originalRayOrigin == otherRay.originalRayOrigin && otherRay != miniWorldRay && otherRay.isContained)
								{
									from = miniWorldRayOrigin;
									to = otherRayOrigin;
									break;
								}
							}
						}

						var pointerLengthDiff = DirectSelection.GetPointerLength(to) - DirectSelection.GetPointerLength(from);
						directSelection.TransferHeldObjects(from, to, Vector3.forward * pointerLengthDiff);

						if (!isContained)
							m_RayWasContained[originalRayOrigin] = false; //Prevent ray from showing
					}

					if (miniWorldRay.dragStartedOutside)
					{
						miniWorldRay.isContained = isContained;
						continue;
					}

					if (miniWorldRayObjects != null && !isContained && wasContained)
					{
						var containedInOtherMiniWorld = false;
						foreach (var world in m_Worlds)
						{
							if (miniWorld != world && world.Contains(originalPointerPosition))
								containedInOtherMiniWorld = true;
						}

						// Transfer objects from miniworld to preview state
						// Don't switch to previewing the objects we are dragging if we are still in another mini world
						if (!containedInOtherMiniWorld)
						{
							// Check for player head
							var playerHead = false;
							foreach (var obj in miniWorldRayObjects)
							{
								if (obj.CompareTag(k_VRPlayerTag))
								{
									playerHead = true;
									directSelection.DropHeldObjects(node);
									break;
								}
							}

							if (!playerHead)
							{
								var scaleFactor = this.GetViewerScale() / miniWorld.referenceTransform.localScale.x;
								miniWorldRay.EnterPreviewMode(miniWorldRayObjects, miniWorldRayOrigin, scaleFactor);
								directSelection.SuspendHoldingObjects(node);
							}
						}
					}

					if (hasPreview)
					{
						// Check if we have just entered another miniworld
						var enterOther = false;
						foreach (var otherRay in m_Rays)
						{
							var otherMiniWorldRay = otherRay.Value;
							if (otherMiniWorldRay.miniWorld != miniWorld && otherMiniWorldRay.miniWorld.Contains(originalPointerPosition))
							{
								miniWorldRay.ExitPreviewMode();
								directSelection.ResumeHoldingObjects(node);
								enterOther = true;
							}
						}

						if (!enterOther)
						{
							if (!isContained)
							{
								miniWorldRay.UpdatePreview();
							}
							else if (!wasContained)
							{
								miniWorldRay.ExitPreviewMode();
								directSelection.ResumeHoldingObjects(node);
							}
						}
					}

					miniWorldRay.isContained = isContained;
				}

				// Update ray visibilities
				Rays.ForEachProxyDevice(data =>
				{
					bool wasContained;
					var rayOrigin = data.rayOrigin;
					m_RayWasContained.TryGetValue(rayOrigin, out wasContained);

					var isContained = false;
					foreach (var miniWorld in m_Worlds)
					{
						isContained |= miniWorld.Contains(rayOrigin.position + rayOrigin.forward * DirectSelection.GetPointerLength(rayOrigin));
					}

					if (isContained && !wasContained)
					{
						Rays.HideRay(rayOrigin, true);
						Rays.LockRay(rayOrigin, this);
					}

					if (!isContained && wasContained)
					{
						Rays.UnlockRay(rayOrigin, this);
						Rays.ShowRay(rayOrigin, true);
					}

					m_RayWasContained[rayOrigin] = isContained;
				});
			}

			internal void OnWorkspaceCreated(IWorkspace workspace)
			{
				var miniWorldWorkspace = workspace as MiniWorldWorkspace;
				if (!miniWorldWorkspace)
					return;

				miniWorldWorkspace.zoomSliderMax = evr.GetModule<SpatialHashModule>().GetMaxBounds().size.MaxComponent()
					/ miniWorldWorkspace.contentBounds.size.MaxComponent();

				var miniWorld = miniWorldWorkspace.miniWorld;
				m_Worlds.Add(miniWorld);

				var intersectionModule = evr.GetModule<IntersectionModule>();
				Rays.ForEachProxyDevice(deviceData =>
				{
					var node = deviceData.node;
					var rayOrigin = deviceData.rayOrigin;
					var proxy = deviceData.proxy;

					var miniWorldRayOrigin = InstantiateMiniWorldRay();
					miniWorldRayOrigin.name = string.Format("{0} Miniworld {1} Ray", node, m_Worlds.Count - 1);
					miniWorldRayOrigin.parent = workspace.transform;

					var tester = miniWorldRayOrigin.GetComponentInChildren<IntersectionTester>();
					tester.active = false;

					m_Rays[miniWorldRayOrigin] = new MiniWorldRay(rayOrigin, miniWorld, proxy, node, deviceData.directSelectInput, tester);

					intersectionModule.AddTester(tester);

					evr.GetModule<HighlightModule>().AddRayOriginForNode(node, miniWorldRayOrigin);

					if (proxy.active)
					{
						if (node == Node.LeftHand)
							miniWorldWorkspace.leftRayOrigin = rayOrigin;

						if (node == Node.RightHand)
							miniWorldWorkspace.rightRayOrigin = rayOrigin;
					}
				}, false);
			}

			internal void OnWorkspaceDestroyed(IWorkspace workspace)
			{
				var miniWorldWorkspace = workspace as MiniWorldWorkspace;
				if (!miniWorldWorkspace)
					return;

				var miniWorld = miniWorldWorkspace.miniWorld;

				// Clean up MiniWorldRays
				m_Worlds.Remove(miniWorld);
				var miniWorldRaysCopy = new Dictionary<Transform, MiniWorldRay>(m_Rays);
				foreach (var ray in miniWorldRaysCopy)
				{
					var miniWorldRay = ray.Value;
					if (miniWorldRay.miniWorld == miniWorld)
						m_Rays.Remove(ray.Key);
				}
			}

			void OnObjectsGrabbed(Transform rayOrigin, HashSet<Transform> grabbedObjects)
			{
				foreach (var ray in m_Rays.Values)
				{
					if (ray.originalRayOrigin == rayOrigin)
						ray.dragStartedOutside = true;
				}
			}

			void OnObjectsDropped(Transform rayOrigin, Transform[] grabbedObjects)
			{
				Node? node = null;
				foreach (var ray in m_Rays)
				{
					var miniWorldRay = ray.Value;
					if (ray.Key == rayOrigin || miniWorldRay.originalRayOrigin == rayOrigin)
					{
						node = miniWorldRay.node;
						break;
					}
				}

				foreach (var ray in m_Rays)
				{
					var miniWorldRay = ray.Value;
					if (miniWorldRay.node == node)
					{
						miniWorldRay.DropPreviewObjects(this);
						miniWorldRay.dragStartedOutside = false;

						if (!miniWorldRay.isContained)
						{
							Rays.UnlockRay(rayOrigin, this);
							Rays.ShowRay(rayOrigin, true);
						}
					}
				}
			}

			public void LateBindInterfaceMethods(DirectSelection provider)
			{
				provider.objectsGrabbed += OnObjectsGrabbed;
				provider.objectsDropped += OnObjectsDropped;
			}
		}
	}
}
#endif
