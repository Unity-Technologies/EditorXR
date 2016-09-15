using System;
using UnityEditor;

namespace UnityEngine.VR.Tools
{
	public interface ITransformTool
	{
		Func <PivotMode> switchOriginMode { get; set; }
		Func <PivotRotation> switchRotationMode { get; set; }
	}
}