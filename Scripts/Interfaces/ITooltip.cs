namespace UnityEngine.Experimental.EditorVR
{
	/// <summary>
	/// Decorates classes which can specify tool tip information
	/// </summary>
	public interface ITooltip
	{
		/// <summary>
		/// The text to display on hover
		/// </summary>
		string tooltipText { get; }

		/// <summary>
		/// The transform relative to which the tooltip will display
		/// </summary>
		Transform tooltipTarget { get; }
	}
}
