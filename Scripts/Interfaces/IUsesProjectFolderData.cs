using System.Collections.Generic;

public interface IUsesProjectFolderData
{
	/// <summary>
	/// Set accessor for folder list data
	/// Used to update existing implementors after lazy load completes
	/// </summary>
	List<FolderData> folderData { set; }
}