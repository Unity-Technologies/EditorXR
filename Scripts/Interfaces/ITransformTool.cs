using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Designates a tool as a Transform tool and allows for its control
	/// </summary>
	public interface ITransformTool
	{
		bool directManipulationEnabled { get; set; }
		void DropHeldObject(Transform obj);
		void TransferObjectToRayOrigin(Transform obj, Transform rayOrigin);
	}
}