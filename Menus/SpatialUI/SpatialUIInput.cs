using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class SpatialUIInput : ActionMapInput {
		public SpatialUIInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @show { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @select { get { return (ButtonInputControl)this[1]; } }
		public ButtonInputControl @cancel { get { return (ButtonInputControl)this[2]; } }
		public AxisInputControl @localRotationZ { get { return (AxisInputControl)this[3]; } }
		public AxisInputControl @localRotationY { get { return (AxisInputControl)this[4]; } }
		public AxisInputControl @localRotationX { get { return (AxisInputControl)this[5]; } }
		public AxisInputControl @localRotationW { get { return (AxisInputControl)this[6]; } }
		public QuaternionInputControl @localRotationQuaternion { get { return (QuaternionInputControl)this[7]; } }
	}
}
