using System.Collections.Generic;
using UnityEngine;

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
	}
}