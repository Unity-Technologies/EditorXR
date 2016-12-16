using UnityEditor;

namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Play")]
	public class Play : BaseAction
	{
		public override void ExecuteAction()
		{
#if UNITY_EDITOR
			EditorApplication.isPlaying = true;
#endif
		}
	}
}