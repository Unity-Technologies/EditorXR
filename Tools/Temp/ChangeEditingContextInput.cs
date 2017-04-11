using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class ChangeEditingContextInput : ActionMapInput {
		public ChangeEditingContextInput (ActionMap actionMap) : base (actionMap) { }
		
		public AxisInputControl @change { get { return (AxisInputControl)this[0]; } }
		public ButtonInputControl @set { get { return (ButtonInputControl)this[1]; } }
	}
}
