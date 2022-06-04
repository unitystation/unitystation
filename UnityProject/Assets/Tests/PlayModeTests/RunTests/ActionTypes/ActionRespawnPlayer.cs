using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;
using Newtonsoft.Json;
using Player;

public partial class TestAction
{
	public bool ShowRespawnPlayer => SpecifiedAction == ActionType.RespawnPlayer;

	[AllowNesting] [ShowIf(nameof(ShowRespawnPlayer))] public RespawnPlayer RespawnPlayerData;


	[System.Serializable]
	public class RespawnPlayer
	{
		public Vector3 PositionToSpawn;
		public Occupation Occupation;
		public string SerialisedCharacterSettings;

		public bool Initiate(TestRunSO TestRunSO)
		{

			CharacterSettings characterSettings;
			if (string.IsNullOrEmpty(SerialisedCharacterSettings))
			{
				characterSettings = new CharacterSettings();
			}
			else
			{
				characterSettings = JsonConvert.DeserializeObject<CharacterSettings>(SerialisedCharacterSettings);
			}

			var Connectedplayer = PlayerList.Instance.Get(PlayerManager.LocalPlayerObject);

			var Request = PlayerSpawnRequest.RequestOccupation( PlayerManager.LocalViewerScript, Occupation, characterSettings,
				Connectedplayer.UserId);


			PlayerSpawn.ServerSpawnPlayer(Request, PlayerManager.LocalViewerScript, Occupation, characterSettings,
				spawnPos : PositionToSpawn.RoundToInt(), existingMind: PlayerManager.LocalPlayerScript.mind,
				conn: Connectedplayer.Connection );

			return true;
		}
	}

	public bool InitiateRespawnPlayer(TestRunSO TestRunSO)
	{
		return RespawnPlayerData.Initiate(TestRunSO);
	}
}
