using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class DirectSelectInput : ActionMapInput {
		public DirectSelectInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @select { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @multiSelect { get { return (ButtonInputControl)this[1]; } }
		public ButtonInputControl @cancel { get { return (ButtonInputControl)this[2]; } }
	}
}
