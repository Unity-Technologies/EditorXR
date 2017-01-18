namespace UnityEngine.Experimental.EditorVR
{
	public interface ITooltip
	{
		string tooltipText { get; }
		Transform tooltipTarget { get; }
	}
}
