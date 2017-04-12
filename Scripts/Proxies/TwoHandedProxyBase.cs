#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	abstract class TwoHandedProxyBase : MonoBehaviour, IProxy
	{
		const int k_RendererQueue = 9000;
		const float k_TargetHighlightThicknessAmount = 0.02f;
		const string k_MaterialAlphaProperty = "_Alpha";
		const string k_MaterialColorTopProperty = "_ColorTop";
		const string k_MaterialColorBottomProperty = "_ColorBottom";
		const string k_MaterialThicknessProperty = "_Thickness";
		const string k_MaterialObjectScaleProperty = "_ObjectScale";

		[SerializeField]
		protected GameObject m_LeftHandProxyPrefab;

		[SerializeField]
		protected GameObject m_RightHandProxyPrefab;

		[SerializeField]
		protected PlayerInput m_PlayerInput;

		[SerializeField]
		protected Material m_HighlightMaterial;

		internal IInputToEvents m_InputToEvents;

		public Transform leftHand
		{
			get { return m_LeftHand; }
		}
		protected Transform m_LeftHand;

		public Transform rightHand
		{
			get { return m_RightHand; }
		}
		protected Transform m_RightHand;
		readonly List<Material> m_Materials = new List<Material>();

		protected Dictionary<Node, Transform> m_RayOrigins;
		protected Dictionary<Node, Material> m_HighlightMaterials;
		protected Dictionary<Node, Transform> m_ProxyMeshRoots;
		Coroutine m_HighlightCoroutine;

		public virtual Dictionary<Node, Transform> rayOrigins { get { return m_RayOrigins; } }

		public virtual TrackedObject trackedObjectInput { protected get; set; }

		public bool active { get { return m_InputToEvents.active; } }

		public event Action activeChanged
		{
			add { m_InputToEvents.activeChanged += value; }
			remove { m_InputToEvents.activeChanged -= value; }
		}

		public virtual bool hidden
		{
			set
			{
				if (value != m_Hidden)
				{
					m_Hidden = value;
					m_LeftHand.gameObject.SetActive(!value);
					m_RightHand.gameObject.SetActive(!value);
				}
			}
		}

		private bool m_Hidden;

		public Dictionary<Transform, Transform> menuOrigins { get; set; }
		public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
		public Dictionary<Transform, Transform> previewOrigins { get; set; }
		public Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }

		public virtual void Awake()
		{
			m_LeftHand = ObjectUtils.Instantiate(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = ObjectUtils.Instantiate(m_RightHandProxyPrefab, transform).transform;
			var leftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			var rightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();

			m_ProxyMeshRoots = new Dictionary<Node, Transform>
			{
				{ Node.LeftHand, leftProxyHelper.meshRoot },
				{ Node.RightHand, rightProxyHelper.meshRoot }
			};

			m_RayOrigins = new Dictionary<Node, Transform>
			{
				{ Node.LeftHand, leftProxyHelper.rayOrigin },
				{ Node.RightHand, rightProxyHelper.rayOrigin }
			};

			menuOrigins = new Dictionary<Transform, Transform>()
			{
				{ leftProxyHelper.rayOrigin, leftProxyHelper.menuOrigin },
				{ rightProxyHelper.rayOrigin, rightProxyHelper.menuOrigin },
			};

			alternateMenuOrigins = new Dictionary<Transform, Transform>()
			{
				{ leftProxyHelper.rayOrigin, leftProxyHelper.alternateMenuOrigin },
				{ rightProxyHelper.rayOrigin, rightProxyHelper.alternateMenuOrigin },
			};

			previewOrigins = new Dictionary<Transform, Transform>
			{
				{ leftProxyHelper.rayOrigin, leftProxyHelper.previewOrigin },
				{ rightProxyHelper.rayOrigin, rightProxyHelper.previewOrigin }
			};

			fieldGrabOrigins = new Dictionary<Transform, Transform>
			{
				{ leftProxyHelper.rayOrigin, leftProxyHelper.fieldGrabOrigin },
				{ rightProxyHelper.rayOrigin, rightProxyHelper.fieldGrabOrigin }
			};
		}

		public virtual IEnumerator Start()
		{
			// In standalone play-mode usage, attempt to get the TrackedObjectInput 
			if (trackedObjectInput == null && m_PlayerInput)
				trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();

			List<Renderer> leftHandRenderers = new List<Renderer>();
			List<Renderer> rightHandRenderers = new List<Renderer>();
			while (leftHandRenderers.Count == 0)
			{
				yield return null;
				foreach (var meshRoot in m_ProxyMeshRoots)
				{
					// Only add device renderers and not anything else that is spawned underneath the hand (e.g. menu button, cone/ray)
					if (meshRoot.Key == Node.LeftHand)
						leftHandRenderers.AddRange(meshRoot.Value.GetComponentsInChildren<Renderer>());
					else if (meshRoot.Key == Node.RightHand)
						rightHandRenderers.AddRange(meshRoot.Value.GetComponentsInChildren<Renderer>());
				}
			}

			foreach (var r in leftHandRenderers)
			{
				m_Materials.AddRange(MaterialUtils.CloneMaterials(r));
			}

			foreach (var r in rightHandRenderers)
			{
				m_Materials.AddRange(MaterialUtils.CloneMaterials(r));
			}

			// Move controllers up into EVR range, so they render properly over our UI (e.g. manipulators)
			foreach (var m in m_Materials)
			{
				m.renderQueue = k_RendererQueue;
			}

			// Perform highlight material setup after default device material setup
			var leftHandHighlightMaterial = Instantiate(m_HighlightMaterial);
			var rightHandHighlightMaterial = Instantiate(m_HighlightMaterial);
			m_HighlightMaterials = new Dictionary<Node, Material>
			{
				{ Node.LeftHand, leftHandHighlightMaterial },
				{ Node.RightHand, rightHandHighlightMaterial }
			};

			// Add materials to collection after controller material queues have been re-assigned; these material queues shouldn't be altered
			m_Materials.Add(leftHandHighlightMaterial);
			m_Materials.Add(rightHandHighlightMaterial);
			//leftHandHighlightMaterial.SetFloat(k_MaterialAlphaProperty, 1f);
			leftHandHighlightMaterial.SetFloat(k_MaterialThicknessProperty, 0f);
			//rightHandHighlightMaterial.SetFloat(k_MaterialAlphaProperty, 1f);
			rightHandHighlightMaterial.SetFloat(k_MaterialThicknessProperty, 0f);

			AssignDeviceHighlightMaterials(Node.LeftHand, leftHandRenderers, leftHandHighlightMaterial);
			AssignDeviceHighlightMaterials(Node.RightHand, rightHandRenderers, rightHandHighlightMaterial);
			Debug.Log("<color=blue>Populate object scale in outline material (shader)</color>");
		}

		public virtual void OnDestroy()
		{
			foreach (var m in m_Materials)
				ObjectUtils.Destroy(m);
		}

		public virtual void OnDisable()
		{
			this.StopCoroutine(ref m_HighlightCoroutine);

			// Set highlight material to hidden state in case it is re-enabled
			if (m_HighlightMaterials != null && m_HighlightMaterials.Any())
			{
				foreach (var pair in m_HighlightMaterials)
				{
					pair.Value.SetFloat(k_MaterialThicknessProperty, 0f);
					break;
				}
			}
		}

		public virtual void Update()
		{
			if (active)
			{
				m_LeftHand.localPosition = trackedObjectInput.leftPosition.vector3;
				m_LeftHand.localRotation = trackedObjectInput.leftRotation.quaternion;

				m_RightHand.localPosition = trackedObjectInput.rightPosition.vector3;
				m_RightHand.localRotation = trackedObjectInput.rightRotation.quaternion;
			}
		}

		void AssignDeviceHighlightMaterials(Node node, List<Renderer> deviceRenderers, Material deviceHighlightMaterial)
		{
			foreach (var meshRoot in m_ProxyMeshRoots)
			{
				// Only add device renderers and not anything else that is spawned underneath the hand (e.g. menu button, cone/ray)
				if (meshRoot.Key == node)
				{
					foreach (var renderer in deviceRenderers)
					{
						var sharedMaterials = renderer.sharedMaterials;
						var materialsWithHighlightAdded = new Material[sharedMaterials.Length + 1];
						for (var i = 0; i < sharedMaterials.Length; i++)
						{
							materialsWithHighlightAdded[i] = sharedMaterials[i];
						}
						materialsWithHighlightAdded[materialsWithHighlightAdded.Length - 1] = deviceHighlightMaterial;
						renderer.sharedMaterials = materialsWithHighlightAdded;
					}
				}
			}
		}

		public void HighlightDevice (Node deviceNode, GradientPair gradientPair)
		{
			if (m_HighlightMaterials == null)
				return;

			Debug.LogWarning("HighlightDevice called!!!");

			Material deviceMaterial = null;
			foreach (var pair in m_HighlightMaterials)
			{
				if (pair.Key == deviceNode)
				{
					deviceMaterial = pair.Value;
					break;
				}
			}

			if (!deviceMaterial)
			{
				Debug.LogWarning("Material was not found for the node : " + deviceNode.ToString());
				return;
			}

			this.RestartCoroutine(ref m_HighlightCoroutine, ShowHighlight(deviceMaterial, gradientPair));
		}

		IEnumerator ShowHighlight(Material deviceMaterial, GradientPair gradientPair)
		{
			// IF the highlight is already running, lerp the gradientPair color to the new target colors
			// If the highlight is not already running, just set the gradientPair colors, then lerp in alpha
			// perform a quick opacity fade in then out of the opacity on the material passed in

			Debug.LogWarning("ShowHighlight called!!!");

			//deviceMaterial.SetFloat(k_MaterialAlphaProperty, 0);
			//deviceMaterial.SetColor(k_MaterialColorTopProperty, m_OriginalInsetGradientPair.a);
			//deviceMaterial.SetColor(k_MaterialColorBottomProperty, m_OriginalInsetGradientPair.b);
			//m_FrameMaterial.SetColor(k_MaterialColorProperty, s_FrameOpaqueColor);
			//deviceMaterial.SetFloat(k_MaterialExpandProperty, 0);
			var currentThickness = deviceMaterial.GetFloat(k_MaterialThicknessProperty);
			var currentTopColor = deviceMaterial.GetColor(k_MaterialColorTopProperty);
			var currentBottomColor = deviceMaterial.GetColor(k_MaterialColorBottomProperty);
			var targetTopColor = gradientPair.a;
			var targetBottomColor = gradientPair.b;
			var duration = 0f;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * 3f;
				var durationShaped = Mathf.Pow(duration, 3);
				var topColor = Color.Lerp(currentTopColor, targetTopColor, durationShaped);
				var bottomColor = Color.Lerp(currentBottomColor, targetBottomColor, durationShaped);
				//deviceMaterial.SetFloat(k_MaterialAlphaProperty, Mathf.Lerp(currentAlpha, 1f, durationShaped));
				deviceMaterial.SetFloat(k_MaterialThicknessProperty, Mathf.Lerp(currentThickness, k_TargetHighlightThicknessAmount, durationShaped));
				deviceMaterial.SetColor(k_MaterialColorTopProperty, topColor);
				deviceMaterial.SetColor(k_MaterialColorBottomProperty, bottomColor);
				yield return null;
			}

			//deviceMaterial.SetFloat(k_MaterialAlphaProperty, 1f);
			deviceMaterial.SetFloat(k_MaterialThicknessProperty, k_TargetHighlightThicknessAmount);
			deviceMaterial.SetColor(k_MaterialColorTopProperty, targetTopColor);
			deviceMaterial.SetColor(k_MaterialColorBottomProperty, targetBottomColor);

			var pauseDuration = 0.5f;
			while (pauseDuration > 0f)
			{
				pauseDuration -= Time.unscaledDeltaTime;
				yield return null;
			}

			while (duration > 0)
			{
				duration -= Time.unscaledDeltaTime * 0.5f;
				var durationShaped = Mathf.Pow(duration, 2);
				deviceMaterial.SetFloat(k_MaterialThicknessProperty, Mathf.Lerp(0f, k_TargetHighlightThicknessAmount, durationShaped));
				yield return null;
			}

			//deviceMaterial.SetFloat(k_MaterialAlphaProperty, 0f);
			deviceMaterial.SetFloat(k_MaterialThicknessProperty, 0f);

			m_HighlightCoroutine = null;
		}
	}
}
#endif
