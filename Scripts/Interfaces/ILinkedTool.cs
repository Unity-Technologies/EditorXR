using System.Collections.Generic;

public interface ILinkedTool
{
	List<ILinkedTool> otherTools { get; }
	bool primary { get; set; }
}
