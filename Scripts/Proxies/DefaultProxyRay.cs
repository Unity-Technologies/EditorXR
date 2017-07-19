#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	sealed class DefaultProxyRay : MonoBehaviour, IUsesViewerScale
	{
		const float k_SetVisibilityDelay = 0.125f;

		class LockObject
		{
			public object lockObject;
			public int priority;
		}

		[SerializeField]
		VRLineRenderer m_LineRenderer;

		[SerializeField]
		GameObject m_Tip;

		[SerializeField]
		float m_LineWidth;

		[SerializeField]
		MeshFilter m_Cone;

		Vector3 m_TipStartScale;
		Transform m_ConeTransform;
		Vector3 m_OriginalConeLocalScale;
		Coroutine m_RayVisibilityCoroutine;
		Coroutine m_ConeVisibilityCoroutine;
		Material m_RayMaterial;
		IntersectionTester m_Tester;
		float m_LastPointerLength;

		float m_SetVisibilityTime = Mathf.Infinity;
		bool m_SetRayVisibile;
		bool m_SetConeVisibile;

		/// <summary>
		/// The object that is set when LockRay is called while the ray is unlocked.
		/// As long as this reference is set, and the ray is locked, only that object can unlock the ray.
		/// If the object reference becomes null, the ray will be free to show/hide/lock/unlock until another locking entity takes ownership.
		/// </summary>
		readonly List<LockObject> m_Locks = new List<LockObject>();
		readonly HashSet<object> m_LockCallers = new HashSet<object>();

		void LockRay(object caller, int priority = 0)
		{
			if (m_LockCallers.Add(caller))
			{
				var maxPriority = 0;
				var firstIndex = -1;
				var count = m_Locks.Count;
				for (var i = 0; i < count; i++)
				{
					var lockObject = m_Locks[i];
					var lockObjectPriority = lockObject.priority;
					if (lockObjectPriority >= maxPriority)
						maxPriority = lockObjectPriority;

					if (firstIndex < 0 && lockObjectPriority >= priority)
						firstIndex = i;
				}

				var newLock = new LockObject { lockObject = caller, priority = priority };
				if (priority > maxPriority || firstIndex == -1)
					m_Locks.Add(newLock);
				else
					m_Locks.Insert(firstIndex, newLock);
			}
			else
			{
				var previousLock = m_Locks.FirstOrDefault(lockObject => lockObject.lockObject == caller);
				if (previousLock.priority != priority)
				{
					m_LockCallers.Remove(caller);
					m_Locks.Remove(previousLock);
					LockRay(caller, priority);
				}
			}
		}

		public void UnlockRay(object caller)
		{
			if (m_LockCallers.Remove(caller))
				m_Locks.RemoveAll(o => o.lockObject == caller);
		}

		bool HasLock(object caller)
		{
			if (m_Locks.Count == 0)
				return true;

			var topLock = m_Locks.Last();
			while (m_Locks.Count > 0 && topLock.lockObject == null)
			{
				m_LockCallers.Remove(topLock.lockObject);
				m_Locks.Remove(topLock);

				if (m_Locks.Count > 0)
				{
					topLock = m_Locks.Last();
				}
				else
				{
					break;
				}
			}

			if (m_Locks.Count == 0)
				return true;

			return topLock.lockObject == caller;
		}

		/// <summary>
		/// The length of the direct selection pointer
		/// </summary>
		public float pointerLength
		{
			get
			{
				if (!coneVisible || m_ConeVisibilityCoroutine != null)
					return m_LastPointerLength;

				m_LastPointerLength = (m_Cone.transform.TransformPoint(m_Cone.sharedMesh.bounds.size.z * Vector3.forward) - m_Cone.transform.position).magnitude;
				return m_LastPointerLength;
			}
		}

		public bool rayVisible { get; private set; }
		public bool coneVisible { get; private set; }

		void OnDisable()
		{
			this.StopCoroutine(ref m_RayVisibilityCoroutine);
			this.StopCoroutine(ref m_ConeVisibilityCoroutine);
		}

		public void SetVisibility(object caller, bool rayVisible, bool coneVisible, int priority = 0)
		{
			LockRay(caller, priority);
			if (isActiveAndEnabled && HasLock(caller))
			{
				Debug.Log("presetvis ray:" + rayVisible + ", cone:" + coneVisible + ", " + this.rayVisible + ", " + this.coneVisible);
				var time = Time.time;
				if (this.rayVisible != rayVisible)
				{
					if (m_SetVisibilityTime == Mathf.Infinity)
					{
						m_SetRayVisibile = rayVisible;
						m_SetVisibilityTime = time;
					}
				}

				if (this.coneVisible != coneVisible)
				{
					if (m_SetVisibilityTime == Mathf.Infinity)
					{
						m_SetVisibilityTime = time;
						m_SetConeVisibile = coneVisible;
					}
				}
			}
		}

		public void SetLength(float length)
		{
			if (!rayVisible)
				return;

			var viewerScale = this.GetViewerScale();
			var scaledWidth = m_LineWidth * viewerScale;
			var scaledLength = length / viewerScale;

			var lineRendererTransform = m_LineRenderer.transform;
			lineRendererTransform.localScale = Vector3.one * scaledLength;
			m_LineRenderer.SetWidth(scaledWidth, scaledWidth * scaledLength);
			m_Tip.transform.position = transform.position + transform.forward * length;
			m_Tip.transform.localScale = scaledLength * m_TipStartScale;
		}

		public void SetColor(Color c)
		{
			m_LineRenderer.SetColors(c, c);
			m_RayMaterial.color = c;
		}

		void Awake()
		{
			m_RayMaterial = MaterialUtils.GetMaterialClone(m_LineRenderer.GetComponent<MeshRenderer>());
			m_ConeTransform = m_Cone.transform;
			m_OriginalConeLocalScale = m_ConeTransform.localScale;
			m_Tester = GetComponentInChildren<IntersectionTester>();

			rayVisible = true;
			coneVisible = true;
		}

		void Start()
		{
			m_TipStartScale = m_Tip.transform.localScale;
			rayVisible = true;
		}

		void Update()
		{
			if (Time.time > m_SetVisibilityTime + k_SetVisibilityDelay)
			{
				if (rayVisible != m_SetRayVisibile)
				{
					rayVisible = m_SetRayVisibile;
					this.StopCoroutine(ref m_RayVisibilityCoroutine);
					m_RayVisibilityCoroutine = StartCoroutine(rayVisible ? ShowRay() : HideRay());
				}

				if (coneVisible != m_SetConeVisibile)
				{
					coneVisible = m_SetConeVisibile;
					this.StopCoroutine(ref m_ConeVisibilityCoroutine);
					m_ConeVisibilityCoroutine = StartCoroutine(coneVisible ? ShowCone() : HideCone());
				}

				Debug.Log("setvis ray:" + rayVisible + ", cone:" + coneVisible);

				m_SetVisibilityTime = Mathf.Infinity;
			}
		}

		IEnumerator HideRay()
		{
			m_Tip.transform.localScale = Vector3.zero;

			// cache current width for smooth animation to target value without snapping
			var currentWidth = m_LineRenderer.widthStart;
			const float kTargetWidth = 0f;
			const float kSmoothTime = 0.1875f;
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			while (currentDuration < kSmoothTime)
			{
				currentDuration += Time.deltaTime;
				currentWidth = MathUtilsExt.SmoothDamp(currentWidth, kTargetWidth, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
				m_LineRenderer.SetWidth(currentWidth, currentWidth);
				yield return null;
			}

			m_LineRenderer.SetWidth(kTargetWidth, kTargetWidth);
			m_RayVisibilityCoroutine = null;
		}

		IEnumerator ShowRay()
		{
			m_Tip.transform.localScale = m_TipStartScale;

			float viewerScale = this.GetViewerScale();
			float scaledWidth;
			var currentWidth = m_LineRenderer.widthStart / viewerScale;
			var smoothVelocity = 0f;
			const float kSmoothTime = 0.3125f;
			var currentDuration = 0f;
			while (currentDuration < kSmoothTime)
			{
				viewerScale = this.GetViewerScale();
				currentDuration += Time.deltaTime;
				currentWidth = MathUtilsExt.SmoothDamp(currentWidth, m_LineWidth, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
				scaledWidth = currentWidth * viewerScale;
				m_LineRenderer.SetWidth(scaledWidth, scaledWidth);
				yield return null;
			}

			viewerScale = this.GetViewerScale();
			scaledWidth = m_LineWidth * viewerScale;
			m_LineRenderer.SetWidth(scaledWidth, scaledWidth);
			m_RayVisibilityCoroutine = null;
		}

		IEnumerator HideCone()
		{
			m_Tester.active = false;
			var currentScale = m_ConeTransform.localScale;
			var smoothVelocity = Vector3.one;
			const float kSmoothTime = 0.1875f;
			var currentDuration = 0f;
			while (currentDuration < kSmoothTime)
			{
				currentDuration += Time.deltaTime;
				currentScale = MathUtilsExt.SmoothDamp(currentScale, Vector3.zero, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
				m_ConeTransform.localScale = currentScale;
				yield return null;
			}

			m_ConeTransform.localScale = Vector3.zero;
			m_ConeVisibilityCoroutine = null;
		}

		IEnumerator ShowCone()
		{
			var currentScale = m_ConeTransform.localScale;
			var smoothVelocity = Vector3.zero;
			const float kSmoothTime = 0.3125f;
			var currentDuration = 0f;
			while (currentDuration < kSmoothTime)
			{
				currentDuration += Time.deltaTime;
				currentScale = MathUtilsExt.SmoothDamp(currentScale, m_OriginalConeLocalScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
				m_ConeTransform.localScale = currentScale;
				yield return null;
			}

			m_ConeTransform.localScale = m_OriginalConeLocalScale;
			m_ConeVisibilityCoroutine = null;
			m_Tester.active = true;
		}

		public Color GetColor()
		{
			return m_RayMaterial.color;
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_RayMaterial);
		}
	}
}
#endif
