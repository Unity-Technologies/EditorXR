using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class MiniWorldInput : ActionMapInput {
		public MiniWorldInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @leftGrab { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @rightGrab { get { return (ButtonInputControl)this[1]; } }
	}
}
