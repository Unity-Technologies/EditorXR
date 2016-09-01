using UnityEngine;

public interface IMenuOrigins
{
	/// <summary>
	/// The transform under which the menu input object should be parented, inheriting position, scale, and rotation
	/// </summary>
	Transform menuInputOrigin { get; set; }

	/// <summary>
	/// The transform under which the menu should be parented, inheriting position and rotation
	/// </summary>
	Transform menuOrigin { get; set; }
}
