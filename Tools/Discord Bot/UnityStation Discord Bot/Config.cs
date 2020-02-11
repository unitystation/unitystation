using System.Collections.Generic;

namespace UnityStation_Discord_Bot
{
	public class Config
	{
		public string SecretKey { get; set; }
		public List<Admin> Admins { get; set; }
		public List<ServerConnection> ServersConnections { get; set; }
	}
}
