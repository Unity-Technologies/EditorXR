using System.Collections.Generic;

/// <summary>
/// Exposes a property used to provide a hierarchy of project folders and assets to the object
/// </summary>
public interface IUsesProjectFolderData
{
	/// <summary>
	/// Set accessor for folder list data
	/// Used to update existing implementors after lazy load completes
	/// </summary>
	List<FolderData> folderData { set; }
}