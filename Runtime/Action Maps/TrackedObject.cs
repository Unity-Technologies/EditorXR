using UnityEngine;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class TrackedObject : ActionMapInput {
		public TrackedObject (ActionMap actionMap) : base (actionMap) { }
		
		public AxisInputControl @leftPositionX { get { return (AxisInputControl)this[0]; } }
		public AxisInputControl @leftPositionY { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @leftPositionZ { get { return (AxisInputControl)this[2]; } }
		public Vector3InputControl @leftPosition { get { return (Vector3InputControl)this[3]; } }
		public AxisInputControl @rightPositionX { get { return (AxisInputControl)this[4]; } }
		public AxisInputControl @rightPositionY { get { return (AxisInputControl)this[5]; } }
		public AxisInputControl @rightPositionZ { get { return (AxisInputControl)this[6]; } }
		public Vector3InputControl @rightPosition { get { return (Vector3InputControl)this[7]; } }
		public AxisInputControl @leftRotationX { get { return (AxisInputControl)this[8]; } }
		public AxisInputControl @leftRotationY { get { return (AxisInputControl)this[9]; } }
		public AxisInputControl @leftRotationZ { get { return (AxisInputControl)this[10]; } }
		public AxisInputControl @leftRotationW { get { return (AxisInputControl)this[11]; } }
		public QuaternionInputControl @leftRotation { get { return (QuaternionInputControl)this[12]; } }
		public AxisInputControl @rightRotationX { get { return (AxisInputControl)this[13]; } }
		public AxisInputControl @rightRotationY { get { return (AxisInputControl)this[14]; } }
		public AxisInputControl @rightRotationZ { get { return (AxisInputControl)this[15]; } }
		public AxisInputControl @rightRotationW { get { return (AxisInputControl)this[16]; } }
		public QuaternionInputControl @rightRotation { get { return (QuaternionInputControl)this[17]; } }
	}
}
