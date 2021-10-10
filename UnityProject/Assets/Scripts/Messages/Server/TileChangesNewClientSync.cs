using System.Linq;
using Mirror;
using Newtonsoft.Json;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

//long name I know. This is for syncing new clients when they join to all of the tile changes
namespace Messages.Server
{
	public class TileChangesNewClientSync : ServerMessage<TileChangesNewClientSync.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string data;
			public uint ManagerSubject;
		}

		//just a best guess, try increasing it until the message exceeds mirror's limit
		private static readonly int MAX_CHANGES_PER_MESSAGE = 20; // 25 caused issues

		public override void Process(NetMessage msg)
		{
			//server doesn't need this message, it messes with its own tiles.
			if (CustomNetworkManager.IsServer) return;
			LoadNetworkObject(msg.ManagerSubject);

			TileChangeManager tm = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
			tm.InitServerSync(msg.data);
		}

		public static void Send(GameObject managerSubject, NetworkConnection recipient, TileChangeList changeList)
		{
			if (changeList == null || changeList.List.Count == 0) return;

			foreach (var changeChunk in changeList.List.ToArray().Chunk(MAX_CHANGES_PER_MESSAGE).Select(TileChangeList.FromList))
			{
				foreach (var entry in changeChunk.List)
				{
					Logger.LogTraceFormat("Sending update for {0} layer {1}", Category.TileMaps, entry.Position,
						entry.LayerType);
				}

				//We use NewtonSoft's Json as unity inbuilt doesnt support nullables
				string jsondata = JsonConvert.SerializeObject(changeChunk, new JsonSerializerSettings()
				{
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
					NullValueHandling = NullValueHandling.Ignore
				});

				NetMessage msg = new NetMessage
				{
					ManagerSubject = managerSubject.GetComponent<NetworkedMatrix>().MatrixSync.netId,
					data = jsondata
				};

				SendTo(recipient, msg);
			}
		}

		public override string ToString()
		{
			return string.Format("[Sync Tile ChangeData]");
		}
	}
}
