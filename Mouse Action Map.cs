using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class MouseActionMap : ActionMapInput {
		public MouseActionMap (ActionMap actionMap) : base (actionMap) { }
		
		public AxisInputControl @mouseDeltaX { get { return (AxisInputControl)this[0]; } }
		public AxisInputControl @mouseDeltaY { get { return (AxisInputControl)this[1]; } }
	}
}
