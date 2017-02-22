using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("SaveScene", "Scene")]
	internal sealed class SaveScene : BaseAction
	{
		public override void ExecuteAction()
		{
			Debug.LogError("ExecuteAction Action should save a scene here");
		}
	}
}