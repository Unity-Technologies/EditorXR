using UnityEditor;

namespace UnityEngine.VR.Actions
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