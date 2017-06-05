#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Paste", ActionMenuItemAttribute.DefaultActionSectionName, 6)]
	sealed class Paste : BaseAction, IUsesSpatialHash, IUsesViewerScale
	{
		public static Transform[] buffer
		{
			get
			{
				return s_Buffer;
			}
			set
			{
				s_Buffer = value;

				if (value != null)
				{
					var bounds = ObjectUtils.GetBounds(value);
					
					s_BufferDistance = bounds.size != Vector3.zero ? (bounds.center - CameraUtils.GetMainCamera().transform.position).magnitude : 0f;
					s_BufferDistance /= IUsesViewerScaleMethods.getViewerScale(); // Normalize this value, so if viewer scale changes when pasted
				}
			}
		}
		static Transform[] s_Buffer;

		static float s_BufferDistance;

		public override void ExecuteAction()
		{
			if (buffer != null)
			{
				var pastedGameObjects = new GameObject[buffer.Length];
				var index = 0;
				var bounds = ObjectUtils.GetBounds(buffer);
				foreach (var transform in buffer)
				{
					var pasted = Instantiate(transform.gameObject);
					var pastedTransform = pasted.transform;
					pasted.hideFlags = HideFlags.None;
					var cameraTransform = CameraUtils.GetMainCamera().transform;
					pastedTransform.position = cameraTransform.TransformPoint(Vector3.forward * s_BufferDistance)
						+ pastedTransform.position - bounds.center;
					pasted.SetActive(true);
					this.AddToSpatialHash(pasted);
					pastedGameObjects[index++] = pasted;
				}

				if (pastedGameObjects.Length > 0)
					Selection.objects = pastedGameObjects;
			}
		}
	}
}
#endif
