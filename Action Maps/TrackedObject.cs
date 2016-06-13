using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
public class TrackedObject : ActionMapInput {
	public TrackedObject (ActionMap actionMap) : base (actionMap) { }
	
	public AxisInputControl @leftPositionX { get { return (AxisInputControl)this[0]; } }
	public AxisInputControl @leftPositionY { get { return (AxisInputControl)this[1]; } }
	public AxisInputControl @leftPositionZ { get { return (AxisInputControl)this[2]; } }
	public Vector3InputControl @leftPosition { get { return (Vector3InputControl)this[3]; } }
}
