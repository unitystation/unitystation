using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems;
using Systems.Spawns;
using Managers;
using Messages.Server;
using Messages.Server.LocalGuiMessages;
using UI.CharacterCreator;

/// <summary>
/// Main API for dealing with spawning players and related things.
/// For spawning of non-player things, see Spawn.
/// </summary>
public static class PlayerSpawn
{
	public class SpawnEventArgs : EventArgs { public GameObject player; }
	public delegate void SpawnHandler(object sender, SpawnEventArgs args);
	public static event SpawnHandler SpawnEvent;

	/// <summary>
	/// Server-side only. For use when a player has only joined (as a JoinedViewer) and
	/// is not in control of any mobs. Spawns the joined viewer as the indicated occupation and transfers control to it.
	/// Note that this doesn't take into account game mode or antags, it just spawns whatever is requested.
	/// </summary>
	/// <param name="request">holds the request data</param>
	/// <param name="joinedViewer">viewer who should control the player</param>
	/// <param name="occupation">occupation to spawn as</param>
	/// <param name="characterSettings">settings to use for the character</param>
	/// <returns>the game object of the spawned player</returns>
	public static GameObject ServerSpawnPlayer(PlayerSpawnRequest request, JoinedViewer joinedViewer, Occupation occupation, CharacterSettings characterSettings, bool showBanner = true)
	{
		if(ValidateCharacter(request) == false)
		{
			return null;
		}

		NetworkConnection conn = joinedViewer.connectionToClient;

		// TODO: add a nice cutscene/animation for the respawn transition
		var newPlayer = ServerSpawnInternal(conn, occupation, characterSettings, null, showBanner: showBanner);
		if (newPlayer != null && occupation.IsCrewmember)
		{
			CrewManifestManager.Instance.AddMember(newPlayer.GetComponent<PlayerScript>(), occupation.JobType);
		}

		if (SpawnEvent != null)
		{
			SpawnEventArgs args = new SpawnEventArgs() { player = newPlayer };
			SpawnEvent.Invoke(null, args);
		}

		return newPlayer;
	}

	private static bool ValidateCharacter(PlayerSpawnRequest request)
	{
		var isOk = true;
		var message = "";

		//Disable this until we fix skin tone checks.
		/*
		if(ServerValidations.HasIllegalSkinTone(request.CharacterSettings))
		{
			message += " Invalid player skin tone.";
			isOk = false;
		}


		if(ServerValidations.HasIllegalCharacterName(request.CharacterSettings.Name))
		{
			message += " Invalid player character name.";
			isOk = false;
		}
		*/
		if(ServerValidations.HasIllegalCharacterAge(request.CharacterSettings.Age))
		{
			message += " Invalid character age.";
			isOk = false;
		}

		if (isOk == false)
		{
			message += " Please change and resave character.";
			ValidateFail(request.JoinedViewer, request.UserID, message);
		}

		return isOk;
	}

	private static void ValidateFail(JoinedViewer joinedViewer, string userId, string message)
	{
		PlayerList.Instance.ServerKickPlayer(userId, message, false, 1, false);
		if(joinedViewer.isServer || joinedViewer.isLocalPlayer)
		{
			joinedViewer.Spectate();
		}
	}

	/// <summary>
	/// Server-side only. For use when a player has only joined (as a JoinedViewer) and
	/// is not in control of any mobs. Spawns the joined viewer as the indicated occupation and transfers control to it.
	/// Note that this doesn't take into account game mode or antags, it just spawns whatever is requested.
	/// </summary>
	/// <param name="spawnRequest">details of the requested spawn</param>
	/// <returns>the game object of the spawned player</returns>
	public static GameObject ServerSpawnPlayer(PlayerSpawnRequest spawnRequest)
	{
		return ServerSpawnPlayer(spawnRequest, spawnRequest.JoinedViewer, spawnRequest.RequestedOccupation,
			spawnRequest.CharacterSettings);
	}

	/// <summary>
	/// For use when player is connected and dead.
	/// Respawns the mind's character and transfers their control to it.
	/// </summary>
	/// <param name="forMind"></param>
	public static void ServerRespawnPlayer(Mind forMind)
	{
		//get the settings from the mind
		var occupation = forMind.occupation;
		var oldBody = forMind.GetCurrentMob();
		var connection = oldBody.GetComponent<NetworkIdentity>().connectionToClient;
		var settings = oldBody.GetComponent<PlayerScript>().characterSettings;

		var player = oldBody.Player();
		var oldGhost = forMind.ghost;
		ServerSpawnInternal(connection, occupation, settings, forMind, willDestroyOldBody: oldGhost != null);

		if (oldGhost)
		{
			_ = Despawn.ServerSingle(oldGhost.gameObject);
		}
	}

	/// <summary>
	/// For use when a player is alive or dead and you want to clone their body and transfer their control to it.
	///
	/// Clones a given mind's current body to a new body and transfers control of the mind's connection to that new body.
	/// </summary>
	/// <param name="forMind"></param>
	/// <param name="worldPosition"></param>
	public static GameObject ServerClonePlayer(Mind forMind, Vector3Int worldPosition)
	{
		//TODO: Can probably remove characterSettings from cloningrecord
		//determine previous occupation / settings
		var oldBody = forMind.GetCurrentMob();
		var occupation = forMind.occupation;
		var connection = oldBody.GetComponent<NetworkIdentity>().connectionToClient;
		var settings = oldBody.GetComponent<PlayerScript>().characterSettings;

		return ServerSpawnInternal(connection, occupation, settings, forMind, worldPosition, false, showBanner: false);
	}

	//Time to start spawning players at arrivals
	private static readonly DateTime ARRIVALS_SPAWN_TIME = new DateTime().AddHours(12).AddMinutes(2);

	/// <summary>
	/// Spawns a new player character and transfers the connection's control into the new body.
	/// If existingMind is null, creates the new mind and assigns it to the new body.
	///
	/// Fires server and client side player spawn hooks.
	/// </summary>
	/// <param name="connection">connection to give control to the new player character</param>
	/// <param name="occupation">occupation of the new player character</param>
	/// <param name="characterSettings">settings of the new player character</param>
	/// <param name="existingMind">existing mind to transfer to the new player, if null new mind will be created
	/// and assigned to the new player character</param>
	/// <param name="spawnPos">world position to spawn at</param>
	/// <param name="spawnItems">If spawning a player, should the player spawn without the defined initial equipment for their occupation?</param>
	/// <param name="willDestroyOldBody">if true, indicates the old body is going to be destroyed rather than pooled,
	/// thus we shouldn't send any network message which reference's the old body's ID since it won't exist.</param>
	///
	/// <returns>the spawned object</returns>
	private static GameObject ServerSpawnInternal(NetworkConnection connection, Occupation occupation, CharacterSettings characterSettings,
		Mind existingMind, Vector3Int? spawnPos = null, bool spawnItems = true, bool willDestroyOldBody = false, bool showBanner = true)
	{
		//determine where to spawn them
		if (spawnPos == null)
		{
			Transform spawnTransform;
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
					"Unable to determine spawn position for connection {0} occupation {1}. Cannot spawn player.",
					Category.EntitySpawn,
					connection.address, occupation.DisplayName);
				return null;
			}

			spawnPos = spawnTransform.transform.position.CutToInt();
		}

		//create the player object
		var newPlayer = ServerCreatePlayer(spawnPos.GetValueOrDefault(), occupation.SpecialPlayerPrefab);
		var newPlayerScript = newPlayer.GetComponent<PlayerScript>();

		//get the old body if they have one.
		var oldBody = existingMind?.GetCurrentMob();

		//transfer control to the player object
		ServerTransferPlayer(connection, newPlayer, oldBody, Event.PlayerSpawned, characterSettings, willDestroyOldBody);

		if (existingMind == null)
		{
			//create the mind of the player
			Mind.Create(newPlayer, occupation);
		}
		else
		{
			//transfer the mind to the new body
			existingMind.SetNewBody(newPlayerScript);
		}


		var ps = newPlayer.GetComponent<PlayerScript>();
		var connectedPlayer = PlayerList.Instance.Get(connection);
		connectedPlayer.Name = ps.playerName;
		connectedPlayer.Job = ps.mind.occupation.JobType;
		UpdateConnectedPlayersMessage.Send();

		//fire all hooks
		var info = SpawnInfo.Player(occupation, characterSettings, CustomNetworkManager.Instance.humanPlayerPrefab,
			SpawnDestination.At(spawnPos), spawnItems: spawnItems);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, newPlayer));

		if (occupation != null && showBanner)
		{
			SpawnBannerMessage.Send(
				newPlayer,
				occupation.DisplayName,
				occupation.SpawnSound.AssetAddress,
				occupation.TextColor,
				occupation.BackgroundColor,
				occupation.PlaySound);
		}
		if (info.SpawnItems)
		{
			newPlayer.GetComponent<DynamicItemStorage>()?.SetUpOccupation(occupation);
		}


		return newPlayer;
	}

	/// <summary>
	/// Use this when a player is currently a ghost and wants to reenter their body.
	/// </summary>
	/// <param name="forConnection">connection to transfer control to</param>
	/// TODO: Remove need for this parameter
	/// <param name="forConnection">object forConnection is currently in control of</param>
	/// <param name="forMind">mind to transfer control back into their body</param>
	public static void ServerGhostReenterBody(NetworkConnection forConnection, GameObject fromObject, Mind forMind)
	{
		var body = forMind.GetCurrentMob();
		var oldGhost = forMind.ghost;
		var ps = body.GetComponent<PlayerScript>();
		var mind = ps.mind;
		var occupation = mind.occupation;
		var settings = ps.characterSettings;
		ServerTransferPlayer(forConnection, body, fromObject, Event.PlayerSpawned, settings, oldGhost != null);
		body.GetComponent<PlayerScript>().playerNetworkActions.ReenterBodyUpdates();

		if (oldGhost)
		{
			_ = Despawn.ServerSingle(oldGhost.gameObject);
		}
	}

	/// <summary>
	/// Use this when a player rejoins the game and already has a logged-out body in the game.
	/// Transfers their control back to the body.
	/// </summary>
	/// <param name="viewer"></param>
	/// <param name="body">to transfer into</param>
	public static void ServerRejoinPlayer(JoinedViewer viewer, GameObject body)
	{
		var ps = body.GetComponent<PlayerScript>();
		var mind = ps.mind;
		var occupation = mind.occupation;
		var settings = ps.characterSettings;
		ServerTransferPlayer(viewer.connectionToClient, body, viewer.gameObject, Event.PlayerRejoined, settings);
		ps = body.GetComponent<PlayerScript>();
		ps.playerNetworkActions.ReenterBodyUpdates();
		ps.mind.ResendSpellActions();
	}

	/// <summary>
	/// Spawns a ghost for the indicated mind's body and transfers the connection's control to it.
	/// </summary>
	/// <param name="conn"></param>
	/// <param name="oldBody"></param>
	/// <param name="characterSettings"></param>
	/// <param name="occupation"></param>
	/// <returns></returns>
	public static void ServerSpawnGhost(Mind forMind)
	{
		if (forMind == null)
		{
			Logger.LogError("Mind was null for ServerSpawnGhost", Category.Ghosts);
			return;
		}
		//determine where to spawn the ghost
		var body = forMind.GetCurrentMob();

		if (body == null)
		{
			Logger.LogError("Body was null for ServerSpawnGhost", Category.Ghosts);
			return;
		}

		var settings = body.GetComponent<PlayerScript>().characterSettings;
		var connection = body.GetComponent<NetworkIdentity>().connectionToClient;
		var registerTile = body.GetComponent<RegisterTile>();
		if (registerTile == null)
		{
			Logger.LogErrorFormat("Cannot spawn ghost for body {0} because it has no registerTile", Category.Ghosts,
				body.name);
			return;
		}

		Vector3Int spawnPosition = TransformState.HiddenPos;
		var objBeh = body.GetComponent<ObjectBehaviour>();
		if (objBeh != null) spawnPosition = objBeh.AssumedWorldPositionServer();

		if (spawnPosition == TransformState.HiddenPos)
		{
			//spawn ghost at occupation location if we can't determine where their body is
			Transform spawnTransform = SpawnPoint.GetRandomPointForJob(forMind.occupation.JobType, true);
			if (spawnTransform == null)
			{
				Logger.LogErrorFormat("Unable to determine spawn position for occupation {1}. Cannot spawn ghost.", Category.Ghosts,
					forMind.occupation.DisplayName);
				return;
			}

			spawnPosition = spawnTransform.transform.position.CutToInt();
		}

		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var ghost = UnityEngine.Object.Instantiate(CustomNetworkManager.Instance.ghostPrefab, spawnPosition, parentTransform.rotation,
			parentTransform);

		forMind.Ghosting(ghost);

		ServerTransferPlayer(connection, ghost, body, Event.GhostSpawned, settings);


		//fire all hooks
		var info = SpawnInfo.Ghost(forMind.occupation, settings, CustomNetworkManager.Instance.ghostPrefab,
			SpawnDestination.At(spawnPosition, parentTransform));
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, ghost));

		if (PlayerList.Instance.IsAdmin(forMind.ghost.connectedPlayer))
		{
			var adminItemStorage = AdminManager.Instance.GetItemSlotStorage(forMind.ghost.connectedPlayer);
			adminItemStorage.ServerAddObserverPlayer(ghost);
			ghost.GetComponent<GhostSprites>().SetAdminGhost();
		}
	}

	/// <summary>
	/// Spawns as a ghost for spectating the Round
	/// </summary>
	public static void ServerSpawnGhost(JoinedViewer joinedViewer, CharacterSettings characterSettings)
	{
		//Hard coding to assistant
		Vector3Int spawnPosition = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT).transform.position.CutToInt();

		//Get spawn location
		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;
		var newPlayer = UnityEngine.Object.Instantiate(CustomNetworkManager.Instance.ghostPrefab, spawnPosition, parentTransform.rotation, parentTransform);

		//Create the mind without a job refactor this to make it as a ghost mind
		Mind.Create(newPlayer);
		ServerTransferPlayer(joinedViewer.connectionToClient, newPlayer, null, Event.GhostSpawned, characterSettings);

		if (PlayerList.Instance.IsAdmin(PlayerList.Instance.Get(joinedViewer.connectionToClient)))
		{
			newPlayer.GetComponent<GhostSprites>().SetAdminGhost();
		}
	}

	/// <summary>
	/// Spawns an assistant dummy
	/// </summary>
	public static void ServerSpawnDummy(Transform spawnTransform = null)
	{
		if(spawnTransform == null)
			spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
		if (spawnTransform != null)
		{
			var dummy = ServerCreatePlayer(spawnTransform.position.RoundToInt());

			CharacterSettings randomSettings = CharacterSettings.RandomizeCharacterSettings();

			ServerTransferPlayer(null, dummy, null, Event.PlayerSpawned, randomSettings);


			//fire all hooks
			var info = SpawnInfo.Player(OccupationList.Instance.Get(JobType.ASSISTANT), randomSettings, CustomNetworkManager.Instance.humanPlayerPrefab,
				SpawnDestination.At(spawnTransform.gameObject));
			Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, dummy));
		}
	}

	/// <summary>
	/// Server-side only. Creates the player object on the server side and fires server-side
	/// spawn hooks. Doesn't transfer control to the client yet. Client side hooks should be fired after client has been
	/// informed of the spawn
	/// </summary>
	/// <param name="spawnWorldPosition">world pos to spawn at</param>
	/// <param name="occupation">occupation to spawn as</param>
	/// <param name="characterSettings">settings to use for the character</param>
	/// <param name="playerPrefab">prefab to spawn for the player</param>
	/// <returns></returns>
	private static GameObject ServerCreatePlayer(Vector3Int spawnWorldPosition, GameObject playerPrefab = null)
	{
		//player is only spawned on server, we don't sync it to other players yet
		var spawnPosition = spawnWorldPosition;
		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;

		if (playerPrefab == null)
		{
			playerPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;
		}

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var player = UnityEngine.Object.Instantiate(playerPrefab, spawnPosition, parentTransform.rotation,
			parentTransform);

		return player;
	}

	public static void ServerTransferPlayerToNewBody(NetworkConnection conn, GameObject newBody, GameObject oldBody,
		Event eventType, CharacterSettings characterSettings, bool willDestroyOldBody = false)
	{
		ServerTransferPlayer(conn, newBody, oldBody, eventType, characterSettings, willDestroyOldBody);
	}

	/// <summary>
	/// Server-side only. Transfers control of a player object to the indicated connection.
	/// </summary>
	/// <param name="conn">The client's NetworkConnection. If logged out the playerlist will return an invalid connectedplayer</param>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="oldBody">The old body of the character.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	/// <param name="characterSettings">settings, ignored if transferring to an existing player body</param>
	/// <param name="willDestroyOldBody">if true, indicates the old body is going to be destroyed rather than pooled,
	/// thus we shouldn't send any network message which reference's the old body's ID since it won't exist.</param>
	private static void ServerTransferPlayer(NetworkConnection conn, GameObject newBody, GameObject oldBody,
		Event eventType, CharacterSettings characterSettings, bool willDestroyOldBody = false)
	{
		if (oldBody)
		{
			var oldPlayerNetworkActions = oldBody.GetComponent<PlayerNetworkActions>();
			if (oldPlayerNetworkActions)
			{
				oldPlayerNetworkActions.RpcBeforeBodyTransfer();
			}

			//no longer can observe their inventory
			oldBody.GetComponent<DynamicItemStorage>()?.ServerRemoveObserverPlayer(oldBody);
		}

		var netIdentity = newBody.GetComponent<NetworkIdentity>();
		if (netIdentity.connectionToClient != null)
		{
			CustomNetworkManager.Instance.OnServerDisconnect(netIdentity.connectionToClient);
		}

		var connectedPlayer = PlayerList.Instance.Get(conn);
		if (connectedPlayer == ConnectedPlayer.Invalid) //this isn't an online player
		{
			PlayerList.Instance.UpdateLoggedOffPlayer(newBody, oldBody);
			NetworkServer.Spawn(newBody);
		}
		else
		{
			PlayerList.Instance.UpdatePlayer(conn, newBody);
			NetworkServer.ReplacePlayerForConnection(conn, newBody);
			//NOTE: With mirror upgrade 04 Feb 2020, it appears we no longer need to do what has been
			//commented out below. Below appears to have been an attempt to give authority back to server
			//But it's implicitly given such authority by the ReplacePlayerForConnection call - that call
			//now removes authority for the player's old object
			// if (oldBody)
			// {
			// 	NetworkServer.ReplacePlayerForConnection(new NetworkConnectionToClient(0), oldBody);
			// }
			TriggerEventMessage.SendTo(newBody, eventType);

			//can observe their new inventory
			var dynamicItemStorage = newBody.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				dynamicItemStorage.ServerAddObserverPlayer(newBody);
				PlayerPopulateInventoryUIMessage.Send(dynamicItemStorage, newBody);
			}

		}

		// If the player is inside a container, send a ClosetHandlerMessage.
		// The ClosetHandlerMessage will attach the container to the transfered player.
		var playerObjectBehavior = newBody.GetComponent<ObjectBehaviour>();
		if (playerObjectBehavior && playerObjectBehavior.parentContainer)
		{
			FollowCameraMessage.Send(newBody, playerObjectBehavior.parentContainer.gameObject);
		}

		if (characterSettings != null)
		{
			var playerScript = newBody.GetComponent<PlayerScript>();
			playerScript.characterSettings = characterSettings;
			playerScript.playerName = playerScript.PlayerState != PlayerScript.PlayerStates.Ai ? characterSettings.Name : characterSettings.AiName;
			newBody.name = characterSettings.Name;
			var playerSprites = newBody.GetComponent<PlayerSprites>();
			if (playerSprites)
			{
				playerSprites.OnCharacterSettingsChange(characterSettings);
			}
		}
	}
}
