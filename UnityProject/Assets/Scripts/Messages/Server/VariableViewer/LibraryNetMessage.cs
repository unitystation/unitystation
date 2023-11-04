using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SecureStuff;
using UnityEngine;

namespace Messages.Server.VariableViewer
{
	public class LibraryNetMessage : ServerMessage<LibraryNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string data;
			public int Number;
		}

		public static List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf> CompressedHierarchy = new List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf>();

		public static int Arrived = 0;
		public override void Process(NetMessage msg)
		{
			//JsonConvert.DeserializeObject<VariableViewerNetworking.NetFriendlyBookShelfView>()
			//Logger.Log(JsonConvert.SerializeObject(data));
			// UIManager.Instance.BookshelfViewer.BookShelfView = msg.data
			CompressedHierarchy.AddRange(JsonConvert.DeserializeObject<List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf>>(msg.data));
			Arrived++;
			if (msg.Number <= Arrived)
			{
				UIManager.Instance.LibraryUI.SetUp(CompressedHierarchy);
				CompressedHierarchy.Clear();
				Arrived = 0;
			}
		}

		public static NetMessage Send(Librarian.Library Library, GameObject ToWho)
		{
			NetMessage msg = new NetMessage();
			var ListsOfLists = VariableViewerNetworking.ProcessLibrary(Library).Chunk(500).ToList();

			foreach (var List in ListsOfLists)
			{
				msg.data = JsonConvert.SerializeObject(List.ToList());
				msg.Number = ListsOfLists.Count;
				SendTo(ToWho, msg, channel: 3);
			}

			return msg;
		}
	}
}