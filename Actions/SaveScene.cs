namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("SaveScene", "Scene")]
	public class SaveScene : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public bool ExecuteAction()
		{
			Debug.LogError("ExecuteAction Action should save a scene here");
			return true;
		}
	}
}