using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Ai;
using Systems.MobAIs;
using UnityEngine;
using Systems.Spawns;
using Objects;

namespace Systems.Teleport
{
	/// <summary>
	/// Class to help with teleportation.
	/// </summary>
	public static class TeleportUtils
	{
		/// <summary>
		/// Gets teleport destinations for all objects with PlayerScript.
		/// </summary>
		/// <returns>TeleportInfo, with name, position and object</returns>
		public static IEnumerable<TeleportInfo> GetMobDestinations()
		{
			var playerBodies = UnityEngine.Object.FindObjectsOfType(typeof(PlayerScript));

			if (playerBodies == null)//If list of PlayerScripts is empty dont run rest of code.
			{
				yield break;
			}

			foreach (PlayerScript player in playerBodies)
			{
				if (player == PlayerManager.LocalPlayerScript)
				{
					continue;
				}

				//Gets Name of Player
				string nameOfObject = player.name;

				if (player.gameObject.name.Length == 0 || player.gameObject.name == null)
				{
					nameOfObject = "Spectator";
				}

				string status;
				//Gets Status of Player
				if (player.IsGhost)
				{
					status = "(Ghost)";
				}
				else if (!player.IsGhost & player.playerHealth.IsDead)
				{
					status = "(Dead)";
				}
				else if (!player.IsGhost)
				{
					status = "(Alive)";
				}
				else
				{
					status = "(Cant tell if Dead/Alive or Ghost)";
				}

				//Gets Position of Player
				var tile = player.gameObject.GetComponent<RegisterTile>();

				var teleportInfo = new TeleportInfo(nameOfObject + "\n" + status, tile.WorldPositionClient, player.gameObject);

				yield return teleportInfo;
			}
		}

		/// <summary>
		/// Gets teleport destinations via all spawn points.
		/// </summary>
		/// <returns>TeleportInfo, with name, position and object</returns>
		public static IEnumerable<TeleportInfo> GetSpawnDestinations()
		{
			var placeGameObjects = UnityEngine.Object.FindObjectsOfType(typeof(SpawnPoint));

			if (placeGameObjects == null)//If list of SpawnPoints is empty dont run rest of code.
			{
				yield break;
			}

			foreach (SpawnPoint place in placeGameObjects)
			{
				var nameOfPlace = place.name;

				if (nameOfPlace.Length == 0)
				{
					nameOfPlace = "Has No Name";
				}

				var placePosition = place.transform.position;// Only way to get position of this object.

				var teleportInfo = new TeleportInfo(nameOfPlace, placePosition.CutToInt(), place.gameObject);

				yield return teleportInfo;
			}
		}

		/// <summary>
		/// Gets teleport destinations via all security cameras.
		/// For AI use only
		/// </summary>
		/// <returns>TeleportInfo, with name, position and object</returns>
		public static IEnumerable<TeleportInfo> GetCameraDestinations()
		{
			if (PlayerManager.LocalPlayer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) yield break;

			var securityCameras = UnityEngine.Object.FindObjectsOfType<SecurityCamera>();

			if (securityCameras == null)
			{
				yield break;
			}

			foreach (var camera in securityCameras)
			{
				if(aiPlayer.OpenNetworks.Contains(camera.SecurityCameraChannel) == false) continue;

				var placePosition = camera.transform.position;// Only way to get position of this object.

				var name = camera.CameraName + " - SecCam";

				if (camera.CameraActive == false)
				{
					name += " INACTIVE";
				}

				var teleportInfo = new TeleportInfo(name, placePosition.CutToInt(), camera.gameObject);

				yield return teleportInfo;
			}
		}

		/// <summary>
		/// Gets teleport destinations via all players in security camera vision.
		/// For AI use only
		/// </summary>
		/// <returns>TeleportInfo, with name, position and object</returns>
		public static IEnumerable<TeleportInfo> GetCameraTrackPlayerDestinations()
		{
			if (PlayerManager.LocalPlayer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) yield break;

			//Check for players
			var playerScripts = UnityEngine.Object.FindObjectsOfType<PlayerScript>();

			if (playerScripts != null)
			{
				foreach (var playerScript in playerScripts)
				{
					if(aiPlayer.CanSeeObject(playerScript.gameObject) == null) continue;

					var placePosition = playerScript.transform.position;// Only way to get position of this object.

					var teleportInfo = new TeleportInfo(playerScript.gameObject.ExpensiveName(), placePosition.CutToInt(), playerScript.gameObject);

					yield return teleportInfo;
				}
			}

			//Check for mobs
			var mobScripts = UnityEngine.Object.FindObjectsOfType<MobAI>();

			if (mobScripts != null)
			{
				foreach (var mobAI in mobScripts)
				{
					if(aiPlayer.CanSeeObject(mobAI.gameObject) == null) continue;

					var placePosition = mobAI.transform.position;// Only way to get position of this object.

					var teleportInfo = new TeleportInfo(mobAI.gameObject.ExpensiveName(), placePosition.CutToInt(), mobAI.gameObject);

					yield return teleportInfo;
				}
			}
		}

		public static void TeleportLocalGhostTo(TeleportInfo teleportInfo)
		{
			var latestPosition = teleportInfo.gameObject.transform.position;
			var playerPosition = PlayerManager.LocalPlayer.gameObject.GetComponent<RegisterTile>().WorldPositionClient;//Finds current player coords

			if (latestPosition != playerPosition)//Spam Prevention
			{
				TeleportLocalGhostTo(latestPosition);
			}
		}

		public static void TeleportLocalGhostTo(Vector3 vector)
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGhostPerformTeleport(vector);
		}

		/// <summary>
		/// Server: instantly teleports the given object to a random point on a circle centred on the object's current position.
		/// </summary>
		/// <param name="objectToTeleport">The object that is being teleported.</param>
		/// <param name="minRadius">The minimum distance the object could teleprot to.</param>
		/// <param name="maxRadius">The maximum distance the object could teleport to.</param>
		/// <param name="tryAvoidSpace">Will perform (limited) rerolling of the random vector generation
		/// until a tile that is not space is found. If the limit is reached, will default to the last space roll.</param>
		/// <param name="tryAvoidImpassable">Like tryAvoidSpace, but checks to see if the tile is passable (not a wall, machine).</param>
		/// <returns>The teleported object's new position.</returns>
		public static Vector3Int ServerTeleportRandom(
				GameObject objectToTeleport, int minRadius = 0, int maxRadius = 16,
				bool tryAvoidSpace = false, bool tryAvoidImpassable = false)
		{
			Vector3Int originalPosition = objectToTeleport.RegisterTile().WorldPositionServer;
			Vector3Int newPosition = GetTeleportPos(originalPosition, minRadius, maxRadius, tryAvoidSpace, tryAvoidImpassable);

			if (objectToTeleport.TryGetComponent(out CustomNetTransform netTransform))
			{
				netTransform.SetPosition(newPosition);
			}
			else if (objectToTeleport.TryGetComponent(out PlayerSync playerSync))
			{
				playerSync.SetPosition(newPosition);
			}
			else
			{
				Logger.LogError($"No transform on {objectToTeleport} - can't teleport!", Category.Movement);
				return originalPosition;
			}

			return newPosition;
		}

		private static Vector3Int GetTeleportPos(Vector3Int centrePoint, float minRadius, float maxRadius, bool avoidSpace, bool avoidImpassable)
		{
			Vector3Int randomVector;
			Vector3Int newPosition = Vector3Int.zero;

			for (int i = 0; i < 8; i++)
			{
				randomVector = (Vector3Int) RandomUtils.RandomAnnulusPoint(minRadius, maxRadius).To2Int();
				newPosition = centrePoint + randomVector;

				if (avoidSpace && MatrixManager.IsSpaceAt(newPosition, CustomNetworkManager.IsServer))
				{
					continue;
				}

				if (avoidImpassable && !MatrixManager.IsPassableAtAllMatricesOneTile(newPosition, CustomNetworkManager.IsServer))
				{
					continue;
				}

				return newPosition;
			}

			return newPosition;
		}
	}
}
