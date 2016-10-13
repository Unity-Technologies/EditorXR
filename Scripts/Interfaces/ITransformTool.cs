using UnityEngine.InputNew;

/// <summary>
/// Designates a tool as a Transform tool and allows for its control
/// </summary>
public interface ITransformTool
{
	DirectSelectInput directSelectInput { get; }
	bool directManipulationEnabled { get; set; }
}