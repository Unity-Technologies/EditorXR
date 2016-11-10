using System;

public interface IProjectFolderList
{
	/// <summary>
	/// Set accessor for folder list data
	/// Used to update existing implementors after lazy load completes
	/// </summary>
	FolderData[] folderData { set; }
}