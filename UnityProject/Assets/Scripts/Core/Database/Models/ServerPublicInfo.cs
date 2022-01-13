using System;
using System.Collections.Generic;

namespace Core.Database.Models
{
	//Read from Streaming Assets/config/serverInfo.json on the server
	[Serializable]
	public class ServerPublicInfo
	{
		// public string RconPass;
		public int RconPort;
		public int ServerPort;
		//CertKey needed in the future for SSL Rcon
		// public string certKey;
		public string ServerName;
		//Location on the internet where clients can be downloaded from:
		public string WinDownload;
		public string OSXDownload;
		public string LinuxDownload;

		//End of a discord invite used for serverinfo page
		public string DiscordLinkID;

		//The Catalogue that the client should load when connecting and the catalogues the server loads on its end
		//Catalogues as in addressable catalogues with content
		public List<string> AddressableCatalogues;

		//Built in catalogue content
		//Such as Lobby music
		public List<string> LobbyAddressableCatalogues;
	}
}