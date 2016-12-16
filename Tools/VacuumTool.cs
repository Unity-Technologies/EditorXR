using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

public class VacuumTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin
{
	float m_LastClickTime;
	readonly Dictionary<Transform, Coroutine> m_VacuumingCoroutines = new Dictionary<Transform, Coroutine>();

	public List<IVacuumable> vacuumables { private get; set; }

	public Transform rayOrigin { get; set; }

	public Vector3 defaultOffset { private get; set; }

	public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
	{
		var standardInput = (Standard)input;
		if (standardInput.action.wasJustPressed)
		{
			if (U.UI.IsDoubleClick(Time.realtimeSinceStartup - m_LastClickTime))
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

				consumeControl(standardInput.action);
			}

			m_LastClickTime = Time.realtimeSinceStartup;
		}
	}

	IEnumerator VacuumToViewer(IVacuumable vacuumable)
	{
		var vacuumTransform = vacuumable.transform;
		//m_Vacuuming = true;
		var startPosition = vacuumTransform.position;
		var startRotation = vacuumTransform.rotation;

		var camera = U.Camera.GetMainCamera().transform;
		var cameraForward = camera.forward;
		cameraForward.y = 0;
		var vacuumForward = vacuumTransform.forward;
		vacuumForward.y = 0;
		var cameraYaw = Quaternion.FromToRotation(vacuumForward, cameraForward);

		var offset = defaultOffset;
		offset.z += vacuumable.vacuumBounds.extents.z;

		var destPosition = camera.position + U.Math.ConstrainYawRotation(camera.rotation) * offset;
		var destRotation = vacuumTransform.rotation * cameraYaw;
		var currentValue = 0f;
		var currentVelocity = 0f;
		var currentDuration = 0f;
		const float kTargetValue = 1f;
		const float kTargetDuration = 0.5f;
		while (currentDuration < kTargetDuration)
		{
			currentDuration += Time.unscaledDeltaTime;
			currentValue = U.Math.SmoothDamp(currentValue, kTargetValue, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
			vacuumTransform.position = Vector3.Lerp(startPosition, destPosition, currentValue);
			vacuumTransform.rotation = Quaternion.Lerp(startRotation, destRotation, currentValue);
			yield return null;
		}

		m_VacuumingCoroutines.Remove(vacuumTransform);
	}
}