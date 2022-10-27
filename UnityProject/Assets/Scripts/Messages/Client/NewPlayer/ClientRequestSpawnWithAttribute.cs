using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using NUnit.Framework;
using ScriptableObjects.Characters;

namespace Messages.Client.NewPlayer
{
	public class ClientRequestSpawnWithAttribute : ClientMessage<ClientRequestSpawnWithAttribute.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string PlayerID;
			public int JoinAttributeID;
			public string JsonCharSettings;
		}

		public override void Process(NetMessage msg)
		{
			var character = JsonConvert.DeserializeObject<CharacterSheet>(msg.JsonCharSettings);
			List<CharacterAttribute> newList = new List<CharacterAttribute>();
			newList.Add(GameManager.Instance.RoundJoinAttributes.AttributesToUse[msg.JoinAttributeID]);
			//TODO: load extra attributes from character settings
			PlayerSpawn.SpawnPlayerV2(character, newList, SentByPlayer.ViewerScript);
		}

		public static NetMessage Send(int joinAttributeID, string jsonCharSettings, string playerID)
		{
			NetMessage msg = new NetMessage
			{
				JoinAttributeID = joinAttributeID,
				JsonCharSettings = jsonCharSettings,
				PlayerID = playerID
			};

			Send(msg);
			return msg;
		}
	}
}