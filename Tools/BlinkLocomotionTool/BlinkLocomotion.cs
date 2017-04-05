using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class BlinkLocomotion : ActionMapInput {
		public BlinkLocomotion (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @blink { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @grip { get { return (ButtonInputControl)this[1]; } }
		public ButtonInputControl @thumb { get { return (ButtonInputControl)this[2]; } }
		public ButtonInputControl @trigger { get { return (ButtonInputControl)this[3]; } }
	}
}
