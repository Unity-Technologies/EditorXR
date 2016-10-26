namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("OpenScene", "Scene")]
	public class OpenScene : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public bool ExecuteAction()
		{
			Debug.LogError("ExecuteAction Action should open a sub-panel showing available scenes to open, if any are found");
			return true;
		}
	}
}