using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class TransformInput : ActionMapInput {
		public TransformInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @pivotMode { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @manipulatorType { get { return (ButtonInputControl)this[1]; } }
		public ButtonInputControl @pivotRotation { get { return (ButtonInputControl)this[2]; } }
	}
}
