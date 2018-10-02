using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DatabaseAPI
{
	public partial class ServerData : MonoBehaviour
	{
		private static ServerData serverData;

		public static ServerData Instance
		{
			get
			{
				if (serverData == null)
				{
					serverData = FindObjectOfType<ServerData>();
				}
				return serverData;
			}
		}

		private const string ServerRoot = "https://dev.unitystation.org"; //dev mode (todo: load release url and key data through build server)
		private const string ApiKey = "77bCwycyzm4wJY5X"; //preloaded for development. Keys are replaced on the server
		private const string URL_UsernameAvailability = ServerRoot + "/validate?key=" + ApiKey + "&username="; //Is the proposed username available
	}
}