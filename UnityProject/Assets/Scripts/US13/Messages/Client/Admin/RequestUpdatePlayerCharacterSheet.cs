using System;
using Logs;
using Mirror;
using Newtonsoft.Json;
using US13.Managers;
using US13.Messages.Server;
using US13.Systems.Lobby;

namespace US13.Messages.Client.Admin
{
	[Obsolete("Remove this as we no longer use IMGUI")]
	public class RequestUpdatePlayerCharacterSheet : ClientMessage<RequestUpdatePlayerCharacterSheet.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string CharacterSheetJson;
			public string AccountID;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.EDIT_PLAYER) == false)
			{
				Loggy.Error().Format("Lacking admin permissions on {}.", Category.Admin, SentByPlayer.Username);
				UpdateTheRequestToCharacterSheetUpdateToRequests.SendSheetUpdate(SentByPlayer, false);
				return;
			}

			PlayerInfo account = null;
			foreach (var player in PlayerList.Instance.AllPlayers)
			{
				if (player.AccountId == msg.AccountID)
				{
					account = player;
					break;
				}
			}

			if (account == null)
			{
				Loggy.Error("Could not find account to update, or lacking admin permissions.");
				UpdateTheRequestToCharacterSheetUpdateToRequests.SendSheetUpdate(SentByPlayer, false);
				return;
			}

			try
			{
				CharacterSheet sheet = JsonConvert.DeserializeObject<CharacterSheet>(msg.CharacterSheetJson);
				account.Mind.CurrentCharacterSettings = sheet;
				UpdateTheRequestToCharacterSheetUpdateToRequests.SendSheetUpdate(SentByPlayer);
			}
			catch (Exception e)
			{
				if (account.Mind == null)
				{
					Loggy.Error("Could not find a mind to link sheet to.");
				}
				else
				{
					Loggy.Error(e.ToString());
				}
				UpdateTheRequestToCharacterSheetUpdateToRequests.SendSheetUpdate(SentByPlayer, false);
			}
		}

		public static NetMessage SendSheetUpdate(string userIDToUpdate, string characterSheetJson)
		{
			var msg = new NetMessage
			{
				CharacterSheetJson = characterSheetJson,
				AccountID = userIDToUpdate,
			};

			Send(msg);
			return msg;
		}
	}

	public class UpdateTheRequestToCharacterSheetUpdateToRequests : ServerMessage<UpdateTheRequestToCharacterSheetUpdateToRequests.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool IsSuccess;
		}

		public override void Process(NetMessage msg)
		{

		}

		public static NetMessage SendSheetUpdate(PlayerInfo requester, bool isSuccess = true)
		{
			var msg = new NetMessage
			{
				IsSuccess = isSuccess
			};

			SendTo(requester, msg);
			return msg;
		}
	}
}