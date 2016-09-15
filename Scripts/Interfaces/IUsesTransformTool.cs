using System;
using UnityEditor;

public interface IUsesTransformTool
{
	Func <PivotMode> switchOriginMode { get; set; }

	Func <PivotRotation> switchRotationMode { get; set; }
}
