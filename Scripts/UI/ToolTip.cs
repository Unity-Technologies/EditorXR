namespace UnityEngine.Experimental.EditorVR.UI
{
	public class Tooltip : MonoBehaviour, ITooltip
	{
		public string tooltipText { get { return m_TooltipText; } set { m_TooltipText = value; } }
		[SerializeField]
		string m_TooltipText;

		public Transform tooltipTarget { get { return m_TooltipTarget; } set { m_TooltipTarget = value; } }
		[SerializeField]
		Transform m_TooltipTarget;

		void Start()
		{
			if (!m_TooltipTarget)
				m_TooltipTarget = transform;
		}
	}
}
