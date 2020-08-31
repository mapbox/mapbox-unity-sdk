using System.Collections.Generic;

public class OfflineMapDownloadInfo
{
	public string MapName;
	public int InitializedTileCount;
	public int SuccesfulTileDownloads = 0;
	public int FailedTileDownloads = 0;
	public List<string> FailedDownloadLogs;

	public OfflineMapDownloadInfo(string name, int tilesCount)
	{
		MapName = name;
		InitializedTileCount = tilesCount;
		FailedDownloadLogs = new List<string>();
	}
}