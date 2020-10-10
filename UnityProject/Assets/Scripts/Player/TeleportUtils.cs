using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	}
}
