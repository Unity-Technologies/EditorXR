using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Play")]
	public class Play : BaseAction
	{
		public override void ExecuteAction()
		{
			EditorApplication.isPlaying = true;
		}
	}
}