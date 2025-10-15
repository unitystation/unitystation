using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gateway;
using Logs;
using MaintRooms;
using NaughtyAttributes;
using ScriptableObjects;
using Systems.Character;
using Systems.GhostRoles;
using Systems.Spawns;
using TileManagement;
using TileMap.Behaviours;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Scenes
{
	public class MaintGeneratorManager : ItemMatrixSystemInit
	{
		[SerializeField] private List<MaintGenerator> maintGenerators = new List<MaintGenerator>();

		[SerializeField] private bool createGhostRoles = false;
		[ShowIf(nameof(createGhostRoles)), SerializeField] private int numberOfGhostRoles = 4;
		[SerializeField] private GhostRoleData maintSurvivorRole = default;
		[SerializeField] private Occupation maintSurvivorOccupation = default;
		private uint maintSurvivorKey = 0;
		private int remainingGhostRoles = 0;

		[SerializeField]
		private PlayerHealthData[] possibleSpecies = null;

		public delegate void GenerationFinishEvent();
		public static GenerationFinishEvent OnGenerationFinish = null;

		[Button("Find Generators in Map")]
		public void FindGeneratorsInMap()
		{
			maintGenerators.Clear();
			MaintGenerator[] mapGenerators = gameObject.transform.parent.GetComponentsInChildren<MaintGenerator>();
			maintGenerators = new List<MaintGenerator>(mapGenerators);
		}

		public override void Initialize()
		{
			base.Initialize();

			TransportUtility.MaintRoomLocations.Clear();
			if (CustomNetworkManager.IsServer == false) return;

			if(createGhostRoles) EventManager.AddHandler(Event.PostRoundStarted, SetUpGhostRoles);

			Chat.AddGameWideSystemMsgToChat($"<color=yellow>Initialising {gameObject.name} Maint Generation</color>");

			Stopwatch sw = new Stopwatch();
			sw.Start();
			RunMaintGenerators();
			sw.Stop();
			Chat.AddGameWideSystemMsgToChat($"<color=green>Initialised {gameObject.name} Maint Generation in {sw.ElapsedMilliseconds}ms</color>");
		}

		private async void RunMaintGenerators()
		{
			foreach (MaintGenerator maintGenerator in maintGenerators)
			{
				if (maintGenerator == null) continue;
				await maintGenerator.InitialiseMaze();

				maintGenerator.CreateTiles();
				maintGenerator.PlaceObjects();
				maintGenerator.LoadRooms();

				maintGenerator.CleanUp();
			}
		}

		private void SetUpGhostRoles()
		{
			Chat.AddGameWideSystemMsgToChat($"<color=yellow>Creating {gameObject.name} Ghost Roles</color>");
			//Create Ghost role entry
			maintSurvivorKey = GhostRoleManager.Instance.ServerCreateRole(maintSurvivorRole);

			//Set the number of roles availiable
			GhostRoleManager.Instance.ServerUpdateRole(maintSurvivorKey, 1, numberOfGhostRoles, -1);
			remainingGhostRoles = numberOfGhostRoles;

			//Add a listener for when a player requests that ghost role
			GhostRoleManager.Instance.serverAvailableRoles[maintSurvivorKey].OnPlayerAdded += SpawnSurvivor;

			Chat.AddGameWideSystemMsgToChat($"<color=green>{gameObject.name} Ghost Roles Created [{numberOfGhostRoles}]</color>");
		}

		public override void OnDestroy()
		{
			//Remove the created ghost role when this is unloaded
			if (createGhostRoles) GhostRoleManager.Instance.ServerRemoveRole(maintSurvivorKey);

			base.OnDestroy();
		}

		//Create a random character sheet for the maints survivor
		private CharacterSheet GenerateSurvivorSheet(PlayerHealthData race)
		{
			var characterSettings = CharacterSheet.GenerateRandomCharacter();
			characterSettings.Species = race.name;
			characterSettings.SerialisedExternalCustom?.Clear();
			characterSettings.SkinTone = CharacterSheet.GetRandomSkinTone(race);
			characterSettings.Name = StringManager.GetRandomName(characterSettings.GetGender(), race.name);
			return characterSettings;
		}

		//Checks to see if both the player and this still exist and haven't been destroyed, else remove the ghost role and fail the check
		private bool RemovePlayer(PlayerInfo player)
		{
			if (this && gameObject) return true;

			GhostRoleManager.Instance.ServerRemoveWaitingPlayer(player);
			Loggy.Error("Ghost role spawn called on null survivor!");
			return false;
		}

		private void SpawnSurvivor(PlayerInfo player)
		{
			//Check if the spawn is valid
			if (RemovePlayer(player) == false) return;

			//Create character sheet
			var characterSettings = GenerateSurvivorSheet(possibleSpecies.PickRandom());

			//Spawn player body
			var survivor = PlayerSpawn.NewSpawnCharacterV2(maintSurvivorOccupation
				,characterSettings);

			//Place player mind in the spawned body
			PlayerSpawn.TransferAccountToSpawnedMind(player, survivor);

			//Appear at survivor spawn position
			survivor.Body.playerMove.AppearAtWorldPositionServer(SpawnPoint.GetRandomPointForJob(JobType.MAINT_SURVIVOR, true).position);

			//Decrease the remaining roles
			GhostRoleManager.Instance.ServerUpdateRole(maintSurvivorKey, 1, --remainingGhostRoles, -1);
			//Remove the player so they can join again once they die
			GhostRoleManager.Instance.ServerRemoveWaitingPlayer(maintSurvivorKey, player);

			Chat.AddExamineMsg(player.GameObject, "You have been wandering the tunnels for hours having finally found a place to rest...");
		}
	}
}
