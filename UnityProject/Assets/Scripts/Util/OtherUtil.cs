using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using Shared.Managers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Util
{
	public static class OtherUtil
	{
		public static List<PlayerInfo> GetVisiblePlayers(Vector2 worldPosition, bool doLinecast = true)
		{
			//Player script is not null for these players
			var players = PlayerList.Instance.InGamePlayers;

			LayerMask layerMask = LayerMask.GetMask( "Door Closed");
			for (int i = players.Count - 1; i > 0; i--)
			{
				if (Vector2.Distance(worldPosition,
					    players[i].Script.PlayerChatLocation.AssumedWorldPosServer()) > 14f)
				{
					//Player in the list is too far away for this message, remove them:
					players.Remove(players[i]);
					continue;
				}

				if (doLinecast == false) continue;

				//within range, but check if they are in another room or hiding behind a wall
				if (MatrixManager.Linecast(worldPosition, LayerTypeSelection.Walls, layerMask,
					    players[i].Script.PlayerChatLocation.AssumedWorldPosServer()).ItHit)
				{
					//if it hit a wall remove that player
					players.Remove(players[i]);
				}
			}

			return players;
		}

		#if UNITY_EDITOR

		private const string MANAGER_PATH = "Assets/Prefabs/SceneConstruction/NestedManagers";

		/// <summary>
		/// Get singleton manager prefab
		/// </summary>
		public static T GetManager<T>(string managerName) where T : SingletonManager<T>
		{
			var managerPrefabGUID = AssetDatabase.FindAssets($"{managerName} t:prefab", new string[] {MANAGER_PATH});
			var managerPrefabPaths = managerPrefabGUID.Select(AssetDatabase.GUIDToAssetPath).ToList();

			if (managerPrefabPaths.Count != 1)
			{
				Loggy.LogError($"Couldn't find {managerName} prefab in specified path, or more than one {managerName} found at: {MANAGER_PATH}");
				return null;
			}

			var gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(managerPrefabPaths.First());
			if (gameManagerPrefab == null)
			{
				Loggy.LogError($"Couldn't find {managerName} prefab in specified path: {MANAGER_PATH}");
			}

			if (gameManagerPrefab.TryGetComponent<T>(out var singletonManager) == false)
			{
				Loggy.LogError($"Couldn't get the component from the specified prefab: {MANAGER_PATH}");
			}

			return singletonManager;
		}

		#endif
	}
}