using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class WorkspaceInput : ActionMapInput {
		public WorkspaceInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @secondaryLeft { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @secondaryRight { get { return (ButtonInputControl)this[1]; } }
	}
}
