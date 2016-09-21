/// <summary>
/// Designates a tool as a Transform tool and allows for its control
/// </summary>
public interface ITransformTool
{
	/// <summary>
	/// Sets the transform mode on this tool
	/// </summary>
	TransformMode mode { set; }
}