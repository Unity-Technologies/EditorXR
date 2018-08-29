using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class SpatialMenuInput : ActionMapInput {
		public SpatialMenuInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @grip { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @select { get { return (ButtonInputControl)this[1]; } }
		public ButtonInputControl @cancel { get { return (ButtonInputControl)this[2]; } }
		public AxisInputControl @localRotationX { get { return (AxisInputControl)this[3]; } }
		public AxisInputControl @localRotationY { get { return (AxisInputControl)this[4]; } }
		public AxisInputControl @localRotationZ { get { return (AxisInputControl)this[5]; } }
		public AxisInputControl @localRotationW { get { return (AxisInputControl)this[6]; } }
		public QuaternionInputControl @localRotationQuaternion { get { return (QuaternionInputControl)this[7]; } }
		public AxisInputControl @localPositionX { get { return (AxisInputControl)this[8]; } }
		public Vector2InputControl @localPositionY { get { return (Vector2InputControl)this[9]; } }
		public AxisInputControl @localPositionZ { get { return (AxisInputControl)this[10]; } }
		public Vector3InputControl @localPosition { get { return (Vector3InputControl)this[11]; } }
		public ButtonInputControl @confirm { get { return (ButtonInputControl)this[12]; } }
		public Vector2InputControl @showMenu { get { return (Vector2InputControl)this[13]; } }
		public AxisInputControl @leftStickX { get { return (AxisInputControl)this[14]; } }
		public AxisInputControl @leftStickY { get { return (AxisInputControl)this[15]; } }
	}
}
