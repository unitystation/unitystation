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
		public GameObject Player;
	}

	public delegate void SpawnHandler(object sender, SpawnEventArgs args);

	public static event SpawnHandler SpawnEvent;


	//Time to start spawning players at arrivals
	private static readonly DateTime ArrivalsSpawnTime = new DateTime().AddHours(12).AddMinutes(2);

	private static Vector3Int GetSpawnPointForOccupation(Occupation occupation)
	{
		Transform spawnTransform;
		if (occupation == null)
		{
			spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
			return spawnTransform != null ? spawnTransform.position.CutToInt() : Vector3Int.zero;
		}

		//Spawn normal location for special jobs or if less than 2 minutes passed
		if (GameManager.Instance.stationTime < ArrivalsSpawnTime || occupation.LateSpawnIsArrivals == false)
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

	public static Mind NewSpawnPlayerV2(PlayerInfo account, Occupation requestedOccupation, CharacterSheet character)
	{
		try
		{
			if (character == null)
			{
				character = CharacterSheet.GenerateRandomCharacter();
			}

			var mind = NewSpawnCharacterV2(requestedOccupation, character);
			TransferAccountToSpawnedMind(account, mind);
			return mind;
		}
		catch (Exception e)
		{
			Logger.LogError(e.ToString());
			return null;
		}

	}



	public static Mind NewSpawnCharacterV2(Occupation requestedOccupation,CharacterSheet character)
	{
		//Validate?
		var mind = SpawnMind(character);
		SpawnAndApplyRole(mind, requestedOccupation, character, SpawnType.NewSpawn);
		return mind;
	}



	public static GameObject RespawnPlayer(Mind mind, Occupation requestedOccupation, CharacterSheet character)
	{
		return SpawnAndApplyRole(mind, requestedOccupation, character, SpawnType.ReSpawn);
	}


	public static GameObject RespawnPlayerAt(Mind mind, Occupation requestedOccupation, CharacterSheet character, Vector3? worldPos)
	{
		var body = SpawnAndApplyRole(mind, requestedOccupation, character, SpawnType.ReSpawn).GetComponent<UniversalObjectPhysics>();
		if (worldPos != null)
		{
			body.AppearAtWorldPositionServer(worldPos.Value);
		}

		return body.gameObject;
	}


	public enum SpawnType
	{
		NewSpawn,
		ReSpawn
	}




	static GameObject SpawnAndApplyRole(Mind mind, Occupation requestedOccupation,
		CharacterSheet character, SpawnType spawnType)
	{
		if (requestedOccupation != null)
		{
			GameObject bodyPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;

			if (requestedOccupation.SpecialPlayerPrefab != null)
			{
				bodyPrefab = requestedOccupation.SpecialPlayerPrefab;
			}

			var body = SpawnPlayerBody(bodyPrefab);

			mind.ApplyOccupation(requestedOccupation); //Probably shouldn't be here?

			//Setup body with custom stuff
			ApplyNewSpawnRoleToBody(body, requestedOccupation, character, spawnType);
			mind.SetPossessingObject(body);
			mind.StopGhosting();

			//get the old body if they have one.
			// var oldBody = existingMind.OrNull()?.GetCurrentMob();

			//transfer control to the player object
			//ServerTransferPlayer(connection, newPlayer, oldBody, Event.PlayerSpawned, toUseCharacterSettings, existingMind);

			return body;
		}
		return null;
	}

	static GameObject SpawnPlayerBody(GameObject bodyPrefab)
	{
		//create the player object

		//player is only spawned on server, we don't sync it to other players yet
		var spawnPosition = Vector3Int.zero;

		if (bodyPrefab == null)
		{
			bodyPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;
		}


		var matrixInfo = MatrixManager.AtPoint(Vector3.zero, true);
		var parentTransform = matrixInfo.Objects;

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var player = UnityEngine.Object.Instantiate(bodyPrefab, Vector3.zero,
			parentTransform.rotation,
			parentTransform);

		//fire all hooks
		var info = SpawnInfo.Ghost(null, bodyPrefab,
			SpawnDestination.At(Vector3.zero, parentTransform));

		NetworkServer.Spawn(player);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, player));
		player.GetComponent<UniversalObjectPhysics>().ForceSetLocalPosition(spawnPosition.ToLocal(matrixInfo.Matrix),
			Vector2.zero, false, matrixInfo.Id, true, 0);
		return player;
	}

	static void ApplyNewSpawnRoleToBody( GameObject body, Occupation requestedOccupation, CharacterSheet character, SpawnType spawnType)
	{
		//Character attributes

		var uop = body.GetComponent<UniversalObjectPhysics>();

		//Character attributes


		//Character attributes
		var name = requestedOccupation.JobType != JobType.AI ? character.Name : character.AiName;
		body.name = name;

		//Character attributes
		var playerSprites = body.GetComponent<PlayerSprites>();
		if (playerSprites)
		{
			// This causes body parts to be made for the race, will cause death if body parts are needed and
			// CharacterSettings is null
			var toUseCharacterSettings = requestedOccupation.UseCharacterSettings ? character : null;
			playerSprites.OnCharacterSettingsChange(toUseCharacterSettings);
		}

		body.GetComponent<DynamicItemStorage>()?.SetUpOccupation(requestedOccupation);
		//determine where to spawn them
		uop.AppearAtWorldPositionServer(GetSpawnPointForOccupation(requestedOccupation));

		switch (spawnType)
		{
			case SpawnType.NewSpawn: //TODO Add more stuff on here
				if (requestedOccupation != null) // && showBanner)?
				{
					SpawnBannerMessage.Send(
						body,
						requestedOccupation.DisplayName,
						requestedOccupation.SpawnSound.AssetAddress,
						requestedOccupation.TextColor,
						requestedOccupation.BackgroundColor,
						requestedOccupation.PlaySound);
				}
				break;
		}
	}

	private static Mind SpawnMind(CharacterSheet character)
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
		var mind = ghost.GetComponent<Mind>();
		var ghosty = ghost.GetComponent<PlayerScript>();

		mind.Ghost();
		mind.SetGhost(ghosty);
		mind.CurrentCharacterSettings = character;

		return mind;
	}

	public static void TransferAccountToSpawnedMind(PlayerInfo account, Mind newMind)
	{
		var isAdmin = account.IsAdmin;
		if (account.Mind != null && isAdmin) //Has old mind
		{
			var adminItemStorage = AdminManager.Instance.GetItemSlotStorage(account);
			adminItemStorage.ServerRemoveObserverPlayer(account.Mind.gameObject);
			account.Mind.GetComponent<GhostSprites>().SetGhostSprite(false);
		}


		TransferAccountOccupyingMind(account, account.Mind, newMind);


		if (isAdmin)
		{
			var adminItemStorage = AdminManager.Instance.GetItemSlotStorage(account);
			adminItemStorage.ServerAddObserverPlayer(newMind.gameObject);
		}

		newMind.GetComponent<GhostSprites>().SetGhostSprite(isAdmin);
	}

	static void TransferAccountOccupyingMind(PlayerInfo account, Mind from, Mind to)
	{
		if (from != null && from != to)
		{
			var oldPlayerNetworkActions = from.GetComponent<PlayerNetworkActions>();
			if (oldPlayerNetworkActions)
			{
				oldPlayerNetworkActions.RpcBeforeBodyTransfer();
			}

			//no longer can observe their inventory
			from.GetComponent<DynamicItemStorage>()?.ServerRemoveObserverPlayer(from.gameObject);

			var leaveInterfaces = from.GetComponents<IOnPlayerLeaveBody>();
			foreach (var leaveInterface in leaveInterfaces)
			{
				leaveInterface.OnPlayerLeaveBody(account);
			}

			from.AccountLeavingMind(account);

			if (account.Connection != null)
			{
				NetworkServer.RemovePlayerForConnection(account.Connection, to.gameObject);
				//
			}
		}

		if (to)
		{
			var netIdentity = to.GetComponent<NetworkIdentity>();
			if (netIdentity.connectionToClient != null)
			{
				CustomNetworkManager.Instance.OnServerDisconnect(netIdentity.connectionToClient);
			}

			if (account.Connection != null)
			{
				NetworkServer.ReplacePlayerForConnection(account.Connection, to.gameObject);
				//TriggerEventMessage.SendTo(To, Event.); //TODO Call this manually bitch
			}

			//can observe their new inventory
			var dynamicItemStorage = to.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				dynamicItemStorage.ServerAddObserverPlayer(to.gameObject);
				PlayerPopulateInventoryUIMessage.Send(dynamicItemStorage,
					to.gameObject); //TODO should we be using the players body as game object???
			}

			// If the player is inside a container, send a ClosetHandlerMessage.
			// The ClosetHandlerMessage will attach the container to the transfered player.
			var playerObjectBehavior = to.GetComponent<UniversalObjectPhysics>();
			if (playerObjectBehavior && playerObjectBehavior.ContainedInObjectContainer)
			{
				FollowCameraMessage.Send(to.gameObject, playerObjectBehavior.ContainedInObjectContainer.gameObject);
			}

			PossessAndUnpossessMessage.Send(to.gameObject, to.gameObject, from.OrNull()?.gameObject);
			var transfers = to.GetComponents<IOnPlayerTransfer>();

			foreach (var transfer in transfers)
			{
				transfer.OnPlayerTransfer(account);
			}
			to.AccountEnteringMind(account);


		}
	}

	public static void TransferOwnershipToConnection(PlayerInfo account, NetworkIdentity from, NetworkIdentity to)
	{
		if (from)
		{
			if (account.Connection != null && from.connectionToClient == account.Connection)
			{
				from.RemoveClientAuthority();
			}
		}

		if (to)
		{
			if (account.Connection != null)
			{
				if (account.Connection.observing.Contains(to) == false)
				{
					account.Connection.observing.Add(to); //TODO because sometimes it cannot be a Observing for some reason , And that causes the ownership message to fail
				}

				to.AssignClientAuthority(account.Connection);
			}
		}

	}
}