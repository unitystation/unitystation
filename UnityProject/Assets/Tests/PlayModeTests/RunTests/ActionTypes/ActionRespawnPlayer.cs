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

		public bool Initiate(TestRunSO testRunSO)
		{
			CharacterSettings characterSettings = string.IsNullOrEmpty(SerialisedCharacterSettings)
					? new CharacterSettings()
					: JsonConvert.DeserializeObject<CharacterSettings>(SerialisedCharacterSettings);

			var playerInfo = PlayerList.Instance.Get(PlayerManager.LocalPlayerObject);
			var spawnRequest = new PlayerSpawnRequest(playerInfo, Occupation, characterSettings);
			
			PlayerSpawn.ServerSpawnPlayer(spawnRequest, PlayerManager.LocalViewerScript, Occupation, characterSettings,
				spawnPos: PositionToSpawn.RoundToInt(), existingMind: PlayerManager.LocalPlayerScript.mind,
				conn: playerInfo.Connection);

			return true;
		}
	}

	public bool InitiateRespawnPlayer(TestRunSO TestRunSO)
	{
		return RespawnPlayerData.Initiate(TestRunSO);
	}
}
