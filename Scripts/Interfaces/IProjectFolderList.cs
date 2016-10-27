using System;

public interface IProjectFolderList
{
	/// <summary>
	/// Set accessor for folder list data
	/// Used to update existing implementors after lazy load completes
	/// </summary>
	FolderData[] folderData { set; }

	/// <summary>
	/// Supplied by ConnectInterfaces to allow getting the current available folder list
	/// </summary>
	Func<FolderData[]> getFolderData { set; }
}