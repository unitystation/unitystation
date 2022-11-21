using System;
using System.Collections.Generic;
using Core.Characters;
using HealthV2;
using Initialisation;
using UnityEngine;
using Mirror;
using Systems;
using Systems.Spawns;
using Managers;
using Messages.Server;
using Messages.Server.LocalGuiMessages;
using Newtonsoft.Json;
using Objects.Research;
using UI.CharacterCreator;
using Player;
using ScriptableObjects.Characters;

/// <summary>
/// This interface will be called after the client has rejoined and has all scenes loaded!
/// </summary>
public interface IOnPlayerRejoin
{
	/// <summary>
	/// Called on server when the player rejoins the game (interface called on the player object)
	/// </summary>
	/// <param name="mind">The mind of the player rejoining</param>
	public void OnPlayerRejoin(Mind mind);
}

/// <summary>
/// This interface will be called when a player is transferred into a new body (but not on rejoin, use above instead)
/// </summary>
public interface IOnPlayerTransfer
{
	/// <summary>
	/// Called on server when the player transfers into a new body (interface called on the new player object)
	/// </summary>
	/// <param name="mind">The mind of the player being transferred</param>
	public void OnPlayerTransfer(PlayerInfo Account);
}

/// <summary>
/// This interface will be called when a player is transferred out of their body
/// </summary>
public interface IOnPlayerLeaveBody
{
	/// <summary>
	/// Called on server when the player leaves a body (interface called on the old player object)
	/// </summary>
	/// <param name="mind">The mind of the player leaving the body</param>
	public void OnPlayerLeaveBody(PlayerInfo Account);
}


public interface IClientPlayerLeaveBody
{
	public void ClientOnPlayerLeaveBody();
}


public interface IClientPlayerTransferProcess
{
	public void ClientOnPlayerTransferProcess();
}

/// <summary>
/// Main API for dealing with spawning players and related things.
/// For spawning of non-player things, see Spawn.
/// </summary>
public static class PlayerSpawn
{
	public class SpawnEventArgs : EventArgs
	{
		public GameObject player;
	}

	public delegate void SpawnHandler(object sender, SpawnEventArgs args);

	public static event SpawnHandler SpawnEvent;


	//Time to start spawning players at arrivals
	private static readonly DateTime ARRIVALS_SPAWN_TIME = new DateTime().AddHours(12).AddMinutes(2);

	private static Vector3Int GetSpawnPointForOccupation(Occupation occupation)
	{
		Transform spawnTransform;
		if (occupation == null)
		{
			spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
			return spawnTransform != null ? spawnTransform.position.CutToInt() : Vector3Int.zero;
		}

		//Spawn normal location for special jobs or if less than 2 minutes passed
		if (GameManager.Instance.stationTime < ARRIVALS_SPAWN_TIME || occupation.LateSpawnIsArrivals == false)
		{
			spawnTransform = SpawnPoint.GetRandomPointForJob(occupation.JobType);
		}
		else
		{
			spawnTransform = SpawnPoint.GetRandomPointForLateSpawn();
			//Fallback to assistant spawn location if none found for late join
			if (spawnTransform == null && occupation.JobType != JobType.NULL)
			{
				spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
			}
		}

		if (spawnTransform == null)
		{
			Logger.LogErrorFormat(
				"Unable to determine spawn position for  occupation {0}. Cannot spawn player.",
				Category.EntitySpawn, occupation.DisplayName);
			return Vector3Int.zero;
		}

		return spawnTransform.transform.position.CutToInt();
	}

	public static Mind NewSpawnPlayerV2(PlayerInfo Account, Occupation requestedOccupation, CharacterSheet character)
	{
		try
		{
			if (character == null)
			{
				character = CharacterSheet.GenerateRandomCharacter();
			}

			var Mind = NewSpawnCharacterV2(requestedOccupation, character);
			TransferAccountToSpawnedMind(Account, Mind);
			return Mind;
		}
		catch (Exception e)
		{
			Logger.LogError(e.ToString());
			return null;
		}

	}



	public static Mind NewSpawnCharacterV2(Occupation requestedOccupation,CharacterSheet character)
	{
		//Can handle spectating
		//TODO events!!!!
		//Validate?
		var mind = SpawnMind(character);
		SpawnAndApplyRole(mind, requestedOccupation, character, SpawnType.NewSpawn);
		return mind;
	}



	public static GameObject RespawnPlayer(Mind mind, Occupation requestedOccupation, CharacterSheet character)
	{
		return SpawnAndApplyRole(mind, requestedOccupation, character, SpawnType.ReSpawn);
	}


	public static GameObject RespawnPlayerAt(Mind mind, Occupation requestedOccupation, CharacterSheet character, Vector3? WorldPOS)
	{
		var Body = SpawnAndApplyRole(mind, requestedOccupation, character, SpawnType.ReSpawn).GetComponent<UniversalObjectPhysics>();
		if (WorldPOS != null)
		{
			Body.AppearAtWorldPositionServer(WorldPOS.Value);
		}

		return Body.gameObject;
	}


	public enum SpawnType
	{
		NewSpawn,
		ReSpawn
	}




	static GameObject SpawnAndApplyRole(Mind mind, Occupation requestedOccupation,
		CharacterSheet character, SpawnType SpawnType)
	{
		if (requestedOccupation != null)
		{
			if (SpawnType == SpawnType.NewSpawn)
			{
				//TODO Set up mind with is traitor and stuff
			}

			GameObject BodyPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;

			if (requestedOccupation.SpecialPlayerPrefab != null)
			{
				BodyPrefab = requestedOccupation.SpecialPlayerPrefab;
			}

			var Body = SpawnPlayerBody(BodyPrefab);

			mind.ApplyOccupation(requestedOccupation); //Probably shouldn't be here?

			//Setup body with custom stuff
			ApplyNewSpawnRoleToBody(Body, requestedOccupation, character, SpawnType);
			mind.SetPossessingObject(Body);
			mind.StopGhosting();
			// LoadManager.RegisterActionDelayed(() =>
			// {
			//
			// }, 1);


			//get the old body if they have one.
			// var oldBody = existingMind.OrNull()?.GetCurrentMob();

			//transfer control to the player object
			//ServerTransferPlayer(connection, newPlayer, oldBody, Event.PlayerSpawned, toUseCharacterSettings, existingMind);



			return Body;
		}

		return null;

	}

	static GameObject SpawnPlayerBody(GameObject BodyPrefab)
	{
		//create the player object

		//player is only spawned on server, we don't sync it to other players yet
		var spawnPosition = Vector3Int.zero;

		if (BodyPrefab == null)
		{
			BodyPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;
		}


		var matrixInfo = MatrixManager.AtPoint(Vector3.zero, true);
		var parentTransform = matrixInfo.Objects;

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var player = UnityEngine.Object.Instantiate(BodyPrefab, Vector3.zero,
			parentTransform.rotation,
			parentTransform);

		//fire all hooks
		var info = SpawnInfo.Ghost(null, BodyPrefab,
			SpawnDestination.At(Vector3.zero, parentTransform));

		NetworkServer.Spawn(player);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, player));
		player.GetComponent<UniversalObjectPhysics>().ForceSetLocalPosition(spawnPosition.ToLocal(matrixInfo.Matrix),
			Vector2.zero, false, matrixInfo.Id, true, 0);
		return player;
	}

	static void ApplyNewSpawnRoleToBody( GameObject Body, Occupation requestedOccupation, CharacterSheet character, SpawnType SpawnType)
	{
		//Character attributes

		var UOP = Body.GetComponent<UniversalObjectPhysics>();

		//Character attributes


		//Character attributes
		var Name = requestedOccupation.JobType != JobType.AI ? character.Name : character.AiName;
		Body.name = Name;

		//Character attributes
		var playerSprites = Body.GetComponent<PlayerSprites>();
		if (playerSprites)
		{
			// This causes body parts to be made for the race, will cause death if body parts are needed and
			// CharacterSettings is null
			var toUseCharacterSettings = requestedOccupation.UseCharacterSettings ? character : null;
			playerSprites.OnCharacterSettingsChange(toUseCharacterSettings);
		}

		Body.GetComponent<DynamicItemStorage>()?.SetUpOccupation(requestedOccupation);
		//determine where to spawn them
		UOP.AppearAtWorldPositionServer(GetSpawnPointForOccupation(requestedOccupation));

		switch (SpawnType)
		{
			case SpawnType.NewSpawn: //TODO Add more stuff on here
				if (requestedOccupation != null) // && showBanner)?
				{
					SpawnBannerMessage.Send(
						Body,
						requestedOccupation.DisplayName,
						requestedOccupation.SpawnSound.AssetAddress,
						requestedOccupation.TextColor,
						requestedOccupation.BackgroundColor,
						requestedOccupation.PlaySound);
				}
				break;
		}
	}

	static Mind SpawnMind(CharacterSheet character)
	{
		var matrixInfo = MatrixManager.AtPoint(Vector3.zero, true);
		var parentTransform = matrixInfo.Objects;

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var ghost = UnityEngine.Object.Instantiate(CustomNetworkManager.Instance.ghostPrefab, Vector3.zero,
			parentTransform.rotation,
			parentTransform);

		//fire all hooks
		var info = SpawnInfo.Ghost(character, CustomNetworkManager.Instance.ghostPrefab,
			SpawnDestination.At(Vector3.zero, parentTransform));

		NetworkServer.Spawn(ghost);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, ghost));

		ghost.name = character.Name;
		var Mind = ghost.GetComponent<Mind>();
		var ghosty = ghost.GetComponent<PlayerScript>();

		Mind.Ghost();
		Mind.SetGhost(ghosty);
		Mind.CurrentCharacterSettings = character;

		return Mind;
	}

	public static void TransferAccountToSpawnedMind(PlayerInfo Account, Mind NewMind)
	{
		var isAdmin = Account.IsAdmin;
		if (Account.Mind != null && isAdmin) //Has old mind
		{
			var adminItemStorage = AdminManager.Instance.GetItemSlotStorage(Account);
			adminItemStorage.ServerRemoveObserverPlayer(Account.Mind.gameObject);
			Account.Mind.GetComponent<GhostSprites>().SetGhostSprite(false);
		}


		TransferAccountOccupyingMind(Account, Account.Mind, NewMind);


		if (isAdmin)
		{
			var adminItemStorage = AdminManager.Instance.GetItemSlotStorage(Account);
			adminItemStorage.ServerAddObserverPlayer(NewMind.gameObject);
		}

		NewMind.GetComponent<GhostSprites>().SetGhostSprite(isAdmin);
	}

	static void TransferAccountOccupyingMind(PlayerInfo Account, Mind From, Mind To)
	{
		if (From != null && From != To)
		{
			var oldPlayerNetworkActions = From.GetComponent<PlayerNetworkActions>();
			if (oldPlayerNetworkActions)
			{
				oldPlayerNetworkActions.RpcBeforeBodyTransfer();
			}

			//no longer can observe their inventory
			From.GetComponent<DynamicItemStorage>()?.ServerRemoveObserverPlayer(From.gameObject);

			var leaveInterfaces = From.GetComponents<IOnPlayerLeaveBody>();
			foreach (var leaveInterface in leaveInterfaces)
			{
				leaveInterface.OnPlayerLeaveBody(Account);
			}

			From.AccountLeavingMind(Account);

			if (Account.Connection != null)
			{
				NetworkServer.RemovePlayerForConnection(Account.Connection, To.gameObject);
				//
			}
		}

		if (To)
		{
			var netIdentity = To.GetComponent<NetworkIdentity>();
			if (netIdentity.connectionToClient != null)
			{
				CustomNetworkManager.Instance.OnServerDisconnect(netIdentity.connectionToClient);
			}

			if (Account.Connection != null)
			{
				NetworkServer.ReplacePlayerForConnection(Account.Connection, To.gameObject);
				//TriggerEventMessage.SendTo(To, Event.); //TODO Call this manually bitch
			}

			//can observe their new inventory
			var dynamicItemStorage = To.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				dynamicItemStorage.ServerAddObserverPlayer(To.gameObject);
				PlayerPopulateInventoryUIMessage.Send(dynamicItemStorage,
					To.gameObject); //TODO should we be using the players body as game object???
			}

			// If the player is inside a container, send a ClosetHandlerMessage.
			// The ClosetHandlerMessage will attach the container to the transfered player.
			var playerObjectBehavior = To.GetComponent<UniversalObjectPhysics>();
			if (playerObjectBehavior && playerObjectBehavior.ContainedInContainer)
			{
				FollowCameraMessage.Send(To.gameObject, playerObjectBehavior.ContainedInContainer.gameObject);
			}

			PossessAndUnpossessMessage.Send(To.gameObject, To.gameObject, From.OrNull()?.gameObject);
			var transfers = To.GetComponents<IOnPlayerTransfer>();

			foreach (var transfer in transfers)
			{
				transfer.OnPlayerTransfer(Account);
			}
			To.AccountEnteringMind(Account);


		}
	}

	public static void TransferOwnershipToConnection(PlayerInfo Account, NetworkIdentity From, NetworkIdentity To)
	{
		if (From)
		{
			if (Account.Connection != null && From.connectionToClient == Account.Connection)
			{
				From.RemoveClientAuthority();
			}
		}

		if (To)
		{
			if (Account.Connection != null)
			{
				if (Account.Connection.observing.Contains(To) == false)
				{
					Account.Connection.observing.Add(To); //TODO because sometimes it cannot be a Observing for some reason , And that causes the ownership message to fail
				}

				To.AssignClientAuthority(Account.Connection);
			}
		}

	}
}