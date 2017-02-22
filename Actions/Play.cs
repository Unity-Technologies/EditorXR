namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Play")]
	internal sealed class Play : BaseAction
	{
		public override void ExecuteAction()
		{
#if UNITY_EDITOR
			EditorApplication.isPlaying = true;
#endif
		}
	}
}