using System;

namespace Core.Database.Models
{
	[Serializable]
	public class ServerStatus
	{
		public string ServerName;
		public string ForkName;
		public int BuildVersion;
		public string CurrentMap;
		public string GameMode;
		public string IngameTime;
		public int PlayerCount;
		public string ServerIP;
		public int ServerPort;
		public string WinDownload;
		public string OSXDownload;
		public string LinuxDownload;
		public int fps;
	}
}