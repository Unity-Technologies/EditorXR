#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class VacuumTool : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, IUsesViewerScale
	{
		[SerializeField]
		ActionMap m_ActionMap;

		float m_LastClickTime;
		readonly Dictionary<Transform, Coroutine> m_VacuumingCoroutines = new Dictionary<Transform, Coroutine>();

		public ActionMap actionMap { get { return m_ActionMap; } }

		public List<IVacuumable> vacuumables { private get; set; }

		public Transform rayOrigin { get; set; }

		public Vector3 defaultOffset { private get; set; }
		public Quaternion defaultTilt { private get; set; }

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var vacuumInput = (VacuumInput)input;
			if (vacuumInput.vacuum.wasJustPressed)
			{
				var realTime = Time.realtimeSinceStartup;
				if (UIUtils.IsDoubleClick(realTime - m_LastClickTime))
				{
					foreach (var vacuumable in vacuumables)
					{
						var vacuumableTransform = vacuumable.transform;
						var ray = new Ray(rayOrigin.position, rayOrigin.forward);
						ray.origin = vacuumableTransform.InverseTransformPoint(ray.origin);
						ray.direction = vacuumableTransform.InverseTransformDirection(ray.direction);
						if (vacuumable.vacuumBounds.IntersectRay(ray))
						{
							Coroutine coroutine;
							if (m_VacuumingCoroutines.TryGetValue(vacuumableTransform, out coroutine))
								StopCoroutine(coroutine);

							m_VacuumingCoroutines[vacuumableTransform] = StartCoroutine(VacuumToViewer(vacuumable));
						}
					}

					consumeControl(vacuumInput.vacuum);
				}

				m_LastClickTime = realTime;
			}
		}

		IEnumerator VacuumToViewer(IVacuumable vacuumable)
		{
			var vacuumTransform = vacuumable.transform;
			var startPosition = vacuumTransform.position;
			var startRotation = vacuumTransform.rotation;

			var offset = defaultOffset;
			offset.z += vacuumable.vacuumBounds.extents.z;
			offset *= this.GetViewerScale();

			var camera = CameraUtils.GetMainCamera().transform;
			var destPosition = camera.position + MathUtilsExt.ConstrainYawRotation(camera.rotation) * offset;
			var destRotation = Quaternion.LookRotation(camera.forward) * defaultTilt;

			var currentValue = 0f;
			var currentVelocity = 0f;
			var currentDuration = 0f;
			const float kTargetValue = 1f;
			const float kTargetDuration = 0.5f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.deltaTime;
				currentValue = MathUtilsExt.SmoothDamp(currentValue, kTargetValue, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
				vacuumTransform.position = Vector3.Lerp(startPosition, destPosition, currentValue);
				vacuumTransform.rotation = Quaternion.Lerp(startRotation, destRotation, currentValue);
				yield return null;
			}

			m_VacuumingCoroutines.Remove(vacuumTransform);
		}
	}
}
#endif
