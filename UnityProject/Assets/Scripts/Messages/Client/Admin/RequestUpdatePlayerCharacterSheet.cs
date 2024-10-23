using System;
using Logs;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using Systems.Character;
using UI.Admin.DIMGUI.Characters;
using UnityEngine;

namespace Messages.Client.Admin
{
	public class RequestUpdatePlayerCharacterSheet : ClientMessage<RequestUpdatePlayerCharacterSheet.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string CharacterSheetJson;
			public string AccountID;
		}

		public override void Process(NetMessage msg)
		{
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
				Loggy.LogError("[RequestUpdatePlayerCharacterSheet] Could not find account to update, or lacking admin permissions.");
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
					Loggy.LogError("[RequestUpdatePlayerCharacterSheet] Could not find a mind to link sheet to.");
				}
				else
				{
					Loggy.LogError(e.ToString());
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
			//(Max): This sucks massive balls for performance. but we can't use netid on UI.
			// at least the client is the one that will handle this, not the server.
			var obj = GameObject.FindObjectsByType<CharacterSheetEditor>(FindObjectsSortMode.InstanceID);
			if (obj == null)
			{
				Loggy.LogError("[UpdateTheRequestToCharacterSheetUpdateToRequests] Could not find editor to update.");
				return;
			}
			if (obj.Length != 0)
			{
				if (msg.IsSuccess)
				{
					obj[0].NetMessage_SuccessEvent();
					Debug.Log("Success");
				}
				else
				{
					obj[0].NetMessage_FailEvent();
				}
			}
		}

		public static NetMessage SendSheetUpdate(PlayerInfo requester, bool isSuccess = true)
		{
			var msg = new NetMessage
			{
				IsSuccess = isSuccess
			};

			Debug.Log(requester.AccountId + isSuccess);

			SendTo(requester, msg);
			return msg;
		}
	}
}