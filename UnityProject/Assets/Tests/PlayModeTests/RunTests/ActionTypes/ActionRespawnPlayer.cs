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
			CharacterSheet characterSettings = string.IsNullOrEmpty(SerialisedCharacterSettings)
					? new CharacterSheet()
					: JsonConvert.DeserializeObject<CharacterSheet>(SerialisedCharacterSettings);

			var playerInfo = PlayerList.Instance.Get(PlayerManager.LocalPlayerObject);
			var spawnRequest = new PlayerSpawnRequest(playerInfo, Occupation, characterSettings);


			var Mind = PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);

			Mind.Body.playerMove.AppearAtWorldPositionServer(PositionToSpawn);

			return true;
		}
	}

	public bool InitiateRespawnPlayer(TestRunSO TestRunSO)
	{
		return RespawnPlayerData.Initiate(TestRunSO);
	}
}
