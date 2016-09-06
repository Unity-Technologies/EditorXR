using UnityEngine;

public interface IMenuOrigins
{
	/// <summary>
	/// The transform under which the menu should be parented
	/// </summary>
	Transform menuOrigin { set; }

	/// <summary>
	/// The transform under which the menu input object should be parented, inheriting position, scale, and rotation
	/// </summary>
	//Transform menuInputOrigin { get; set; }

	/// <summary>
	/// The transform under which the alternate menu should be parented
	/// </summary>
	Transform alternateMenuOrigin { set; }
}
