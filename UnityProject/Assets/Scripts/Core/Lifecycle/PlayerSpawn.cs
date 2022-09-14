using System;
using System.Collections.Generic;
using HealthV2;
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
	public void OnPlayerTransfer(Mind mind);
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
	public void OnPlayerLeaveBody(Mind mind);
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
	public static GameObject ServerSpawnPlayer(PlayerSpawnRequest request, JoinedViewer joinedViewer,
		Occupation occupation, CharacterSheet characterSettings, bool showBanner = true, Vector3Int?
			spawnPos = null, Mind existingMind = null, NetworkConnectionToClient conn = null)
	{
		if (ValidateCharacter(request) == false)
		{
			return null;
		}

		if (conn == null)
		{
			conn = joinedViewer.connectionToClient;
		}


		// TODO: add a nice cutscene/animation for the respawn transition
		var newPlayer = ServerSpawnInternal(conn, occupation, characterSettings, existingMind, showBanner: showBanner,
			spawnPos: spawnPos);
		if (newPlayer != null && occupation.IsCrewmember)
		{
			CrewManifestManager.Instance.AddMember(newPlayer.GetComponent<PlayerScript>(), occupation.JobType);
		}

		if (SpawnEvent != null)
		{
			SpawnEventArgs args = new SpawnEventArgs() {player = newPlayer};
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

		if (ServerValidations.HasIllegalCharacterAge(request.CharacterSettings.Age))
		{
			message += " Invalid character age.";
			isOk = false;
		}
		*/

		if (isOk == false)
		{
			message += " Please change and resave character.";
			ValidateFail(request.Player, message);
		}

		return isOk;
	}

	private static void ValidateFail(PlayerInfo player, string message)
	{
		PlayerList.Instance.ServerKickPlayer(player, message);
		if (player.ViewerScript.isServer || player.ViewerScript.isLocalPlayer)
		{
			player.ViewerScript.Spectate();
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
		return ServerSpawnPlayer(spawnRequest, spawnRequest.Player.ViewerScript, spawnRequest.RequestedOccupation,
			spawnRequest.CharacterSettings);
	}

	/// <summary>
	/// For use when player is connected and dead.
	/// Respawns the mind's character and transfers their control to it.
	/// </summary>
	/// <param name="forMind"></param>
	/// <param name="spawnPos">Override for spawn pos, null to spawn at normal spawnpoint</param>
	public static void ServerRespawnPlayer(Mind forMind, Vector3Int? spawnPos = null)

	{
		//get the settings from the mind
		var occupation = forMind.occupation;
		var oldBody = forMind.GetCurrentMob();
		var connection = oldBody.GetComponent<NetworkIdentity>().connectionToClient;
		var settings = forMind.CurrentCharacterSettings;

		var player = oldBody.Player();
		var oldGhost = forMind.ghost;

		ServerSpawnInternal(connection, occupation, settings, forMind, spawnPos);
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

	private static Vector3Int GetSpawnPointForOccupation(Occupation occupation)
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
				"Unable to determine spawn position for  occupation {0}. Cannot spawn player.",
				Category.EntitySpawn, occupation.DisplayName);
			return Vector3Int.zero;
		}

		return spawnTransform.transform.position.CutToInt();
	}

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
	/// <returns>the spawned object</returns>
	private static GameObject ServerSpawnInternal(NetworkConnectionToClient connection, Occupation occupation,
		CharacterSheet characterSettings,
		Mind existingMind, Vector3Int? spawnPos = null, bool spawnItems = true,
		bool showBanner = true)
	{
		//determine where to spawn them
		if (spawnPos == null)
		{
			spawnPos = GetSpawnPointForOccupation(occupation);
		}


		if (existingMind == null)
		{
			//Spawn ghost
			var Playerinfo = PlayerList.Instance.GetOnline(connection);
			var ghosty = ServerSpawnGhost(Playerinfo, spawnPos.Value, characterSettings);

			existingMind = ghosty.GetComponent<Mind>();
			existingMind.occupation = occupation;

		}


		//create the player object
		var newPlayer = ServerCreatePlayer(spawnPos.GetValueOrDefault(), occupation.SpecialPlayerPrefab);
		var newPlayerScript = newPlayer.GetComponent<PlayerScript>();

		//get the old body if they have one.
		var oldBody = existingMind.OrNull()?.GetCurrentMob();

		var toUseCharacterSettings = occupation.UseCharacterSettings ? characterSettings : null;


		//transfer control to the player object
		ServerTransferPlayer(connection, newPlayer, oldBody, Event.PlayerSpawned, toUseCharacterSettings, existingMind);


		if (existingMind != null)
		{
			//transfer the mind to the new body
			existingMind.SetNewBody(newPlayerScript);
		}

		var ps = newPlayer.GetComponent<PlayerScript>();
		var connectedPlayer = PlayerList.Instance.GetOnline(connection);
		connectedPlayer.Name = ps.playerName;
		connectedPlayer.Job = ps.mind.occupation.JobType;
		UpdateConnectedPlayersMessage.Send();

		//fire all hooks
		var info = SpawnInfo.Player(occupation, characterSettings, CustomNetworkManager.Instance.humanPlayerPrefab,
			SpawnDestination.At(spawnPos), spawnItems: spawnItems);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, newPlayer));

		var Race = characterSettings.GetRaceSo().Base.BrainPrefab;
		var brain =  Spawn.ServerPrefab(Race).GameObject;

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


		ps.characterSettings = characterSettings;
		ps.playerName = ps.PlayerType != PlayerTypes.Ai
			? characterSettings.Name
			: characterSettings.AiName;
		newPlayer.name = ps.playerName;
		var playerSprites = newPlayer.GetComponent<PlayerSprites>();
		if (playerSprites)
		{
			// This causes body parts to be made for the race, will cause death if body parts are needed and
			// CharacterSettings is null
			playerSprites.OnCharacterSettingsChange(characterSettings);
		}


		var Head = newPlayer.GetComponent<LivingHealthMasterBase>().GetFirstBodyPartInArea(BodyPartType.Head);

		Head.OrganStorage.ServerTryAdd(brain);
		existingMind.SetPossessingObject(brain);

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
	public static void ServerGhostReenterBody(NetworkConnectionToClient forConnection, GameObject fromObject,
		Mind forMind)
	{
		var body = forMind.GetCurrentMob();
		var oldGhost = forMind.ghost;
		var ps = body.GetComponent<PlayerScript>();
		var mind = ps.mind;
		var occupation = mind.occupation.OrNull();
		var settings = ps.characterSettings;

		if (ps.connectionToClient != null)
		{
			Logger.LogError(
				$"There was already a connection in {body.ExpensiveName()} for {forMind.ghost.gameObject.ExpensiveName()}!");
			return;
		}

		ServerTransferPlayer(forConnection, body, fromObject, Event.PlayerSpawned, settings, forMind);
		body.GetComponent<PlayerScript>().playerNetworkActions.ReenterBodyUpdates();
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
		var settings = ps.characterSettings;

		if (ps.mind?.occupation != null && ps.mind.occupation.UseCharacterSettings == false)
		{
			settings = null;
		}

		ServerTransferPlayer(viewer.connectionToClient, body, viewer.gameObject, Event.PlayerRejoined, settings,
			ps.mind);
		ps = body.GetComponent<PlayerScript>();
		ps.playerNetworkActions.ReenterBodyUpdates();

		var rejoins = body.GetComponents<IOnPlayerRejoin>();
		foreach (var rejoin in rejoins)
		{
			rejoin.OnPlayerRejoin(ps.mind);
		}
	}

	public static void ServerGhost(Mind forMind)
	{
		forMind.ghost.gameObject.GetComponent<GhostMove>()
			.ForcePositionClient(forMind.body.AssumedWorldPos, false, false);
		forMind.Ghosting(forMind.ghost.gameObject);
		var settings = forMind.body.GetComponent<PlayerScript>().characterSettings;
		var connection = forMind.body.GetComponent<NetworkIdentity>().connectionToClient;

		ServerTransferPlayer(connection, forMind.ghost.gameObject, forMind.body.gameObject, Event.GhostSpawned,
			settings, forMind);
	}

	/// <summary>
	/// Spawns a ghost for the indicated mind's body and transfers the connection's control to it.
	/// </summary>
	/// <param name="conn"></param>
	/// <param name="oldBody"></param>
	/// <param name="characterSettings"></param>
	/// <param name="occupation"></param>
	/// <returns></returns>
	private static PlayerScript ServerSpawnGhost(PlayerInfo playerInfo, Vector3Int spawnPosition,
		CharacterSheet characterSettings)
	{
		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var ghost = UnityEngine.Object.Instantiate(CustomNetworkManager.Instance.ghostPrefab, spawnPosition,
			parentTransform.rotation,
			parentTransform);

		//fire all hooks
		var info = SpawnInfo.Ghost(characterSettings, CustomNetworkManager.Instance.ghostPrefab,
			SpawnDestination.At(spawnPosition, parentTransform));
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, ghost));

		var isAdmin = playerInfo.IsAdmin;
		if (isAdmin)
		{
			var adminItemStorage = AdminManager.Instance.GetItemSlotStorage(playerInfo);
			adminItemStorage.ServerAddObserverPlayer(ghost);
		}

		//Set ghost sprite
		ghost.GetComponent<GhostSprites>().SetGhostSprite(isAdmin);

		ghost.name = characterSettings.Name;
		var existingMind = ghost.GetComponent<Mind>();
		var ghosty = ghost.GetComponent<PlayerScript>();

		existingMind.SetGhost(ghosty);
		existingMind.IsGhosting = true;
		existingMind.CurrentCharacterSettings = characterSettings;
		playerInfo.SetMind(existingMind);
		return ghosty;
	}


	/// <summary>
	/// Spawns as a ghost for spectating the Round
	/// </summary>
	public static void ServerNewPlayerSpectate(JoinedViewer joinedViewer, CharacterSheet characterSettings)
	{
		//Hard coding to assistant
		Vector3Int spawnPosition = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT).transform.position.CutToInt();

		//Spawn ghost
		var Playerinfo = PlayerList.Instance.GetOnline(joinedViewer.connectionToClient);
		var ghosty = ServerSpawnGhost(Playerinfo, spawnPosition, characterSettings);

		//Create the mind without a job refactor this to make it as a ghost mind
		ServerTransferPlayer(joinedViewer.connectionToClient, ghosty.gameObject, null, Event.GhostSpawned, characterSettings,
			ghosty.mind);

		var isAdmin = PlayerList.Instance.GetOnline(joinedViewer.connectionToClient).IsAdmin;
		ghosty.gameObject.GetComponent<GhostSprites>().SetGhostSprite(isAdmin);
	}

	/// <summary>
	/// Spawns an assistant dummy
	/// </summary>
	public static void ServerSpawnDummy(Transform spawnTransform = null)
	{
		if (spawnTransform == null)
		{
			spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
		}

		if (spawnTransform != null)
		{
			var dummy = ServerCreatePlayer(spawnTransform.position.RoundToInt());
			CharacterSheet randomSettings = CharacterSheet.GenerateRandomCharacter();
			ServerTransferPlayer(null, dummy, null, Event.PlayerSpawned, randomSettings, null);

			//fire all hooks
			var info = SpawnInfo.Player(OccupationList.Instance.Get(JobType.ASSISTANT), randomSettings,
				CustomNetworkManager.Instance.humanPlayerPrefab,
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
		player.GetComponent<UniversalObjectPhysics>().ForceSetLocalPosition(spawnPosition.ToLocal(matrixInfo.Matrix),
			Vector2.zero, false, matrixInfo.Id, true, 0);


		return player;
	}

	public static void ServerTransferPlayerToNewBody(NetworkConnectionToClient conn, Mind mind, GameObject newBody,
		Event eventType,
		CharacterSheet characterSettings)
	{
		//get the old body if they have one.
		var oldBody = mind.body.OrNull()?.gameObject;

		if (mind.occupation != null && mind.occupation.UseCharacterSettings == false)
		{
			characterSettings = null;
		}

		ServerTransferPlayer(conn, newBody, oldBody, eventType, characterSettings, mind);

		var newPlayerScript = newBody.GetComponent<PlayerScript>();

		//transfer the mind to the new body
		mind.SetNewBody(newPlayerScript);
	}

	/// <summary>
	/// Server-side only. Transfers control of a player object to the indicated connection.
	/// </summary>
	/// <param name="conn">The client's NetworkConnection. If logged out the playerlist will return an invalid connectedplayer</param>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="oldBody">The old body of the character.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	/// <param name="characterSettings">settings, ignored if transferring to an existing player body</param>
	/// <param name="mind">mind of the player transferred</param>
	/// <param name="willDestroyOldBody">if true, indicates the old body is going to be destroyed rather than pooled,
	/// thus we shouldn't send any network message which reference's the old body's ID since it won't exist.</param>
	private static void ServerTransferPlayer(NetworkConnectionToClient conn, GameObject newBody, GameObject oldBody,
		Event eventType, CharacterSheet characterSettings, Mind mind)
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

			var leaveInterfaces = oldBody.GetComponents<IOnPlayerLeaveBody>();
			foreach (var leaveInterface in leaveInterfaces)
			{
				leaveInterface.OnPlayerLeaveBody(mind);
			}
		}

		var netIdentity = newBody.GetComponent<NetworkIdentity>();
		if (netIdentity.connectionToClient != null)
		{
			CustomNetworkManager.Instance.OnServerDisconnect(netIdentity.connectionToClient);
		}

		var connectedPlayer = PlayerList.Instance.GetOnline(conn);
		if (connectedPlayer == PlayerInfo.Invalid) //this isn't an online player
		{
			PlayerList.Instance.UpdateLoggedOffPlayer(newBody, oldBody);
			NetworkServer.Spawn(newBody);
		}
		else
		{
			PlayerList.Instance.UpdatePlayer(conn, newBody);
			NetworkServer.ReplacePlayerForConnection(conn, newBody);

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
		var playerObjectBehavior = newBody.GetComponent<UniversalObjectPhysics>();
		if (playerObjectBehavior && playerObjectBehavior.ContainedInContainer)
		{
			FollowCameraMessage.Send(newBody, playerObjectBehavior.ContainedInContainer.gameObject);
		}

		var transfers = newBody.GetComponents<IOnPlayerTransfer>();
		foreach (var transfer in transfers)
		{
			transfer.OnPlayerTransfer(mind);
		}
	}
}