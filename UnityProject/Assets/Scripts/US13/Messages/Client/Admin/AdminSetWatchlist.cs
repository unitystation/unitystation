using System.Collections.Generic;
using System.IO;
using Mirror;
using Newtonsoft.Json;
using SecureStuff;
using US13.Core.Admin.Logs;

namespace US13.Messages.Client.Admin
{
	public class AdminSetWatchlist: ClientMessage<AdminSetWatchlist.NetMessage>
	{

		private static Dictionary<string, bool> watchlist;

		public static Dictionary<string, bool> Watchlist
		{
			get
			{
				if (watchlist == null)
				{
					var NotePath = Path.Combine(AccessFile.AdminFolder + "Watchlist" , "Watchlist");
					if (AccessFile.Exists(NotePath, true, FolderType.Logs) == false)
					{
						AccessFile.Save(NotePath, JsonConvert.SerializeObject(new Dictionary<string, bool>()), FolderType.Logs);
					}

					var data = AccessFile.Load(NotePath, FolderType.Logs);
					watchlist= JsonConvert.DeserializeObject< Dictionary<string, bool>>( data);
					if (watchlist == null)
					{
						watchlist = new Dictionary<string, bool>();
					}
				}

				return watchlist;
			}
		}

		public struct NetMessage : NetworkMessage
		{
			public bool Watchlist;
			public string AccountID;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_INFO))
			{
				AdminLogsManager.AddNewLog( "Admin " , SentByPlayer?.GameObject,$" Set Watchlist for AccountID {msg.AccountID} to  Watchlist > {msg.Watchlist}", LogCategory.Admin);
				var NotePath = Path.Combine(AccessFile.AdminFolder + "Watchlist" , "Watchlist");
				if (AccessFile.Exists(NotePath, true, FolderType.Logs) == false)
				{
					AccessFile.Save(NotePath, JsonConvert.SerializeObject(new Dictionary<string, bool>()), FolderType.Logs);
				}


				var Dictionary = JsonConvert.DeserializeObject< Dictionary<string, bool>>( AccessFile.Load(NotePath, FolderType.Logs));
				Dictionary[msg.AccountID] = msg.Watchlist;
				watchlist = Dictionary;
				AccessFile.Save(NotePath, JsonConvert.SerializeObject(Dictionary), FolderType.Logs);

			}
		}

		public static NetMessage Send(bool OnWatchlist, string AccountID)
		{
			NetMessage msg = new()
			{
				Watchlist = OnWatchlist,
				AccountID = AccountID
			};

			Send(msg);
			return msg;
		}
	}
}
