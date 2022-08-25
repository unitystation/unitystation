using System.Linq;
using Construction;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	/// <summary>
	/// Client requests to construct something using the material in their active hand.
	/// </summary>
	public class RequestBuildMessage : ClientMessage<RequestBuildMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			//index of the entry in the ConstructionList.
			public byte EntryIndex;
		}

		public override void Process(NetMessage msg)
		{
			var playerScript = SentByPlayer.Script;
			var playerObject = SentByPlayer.GameObject;
			var clientStorage = playerScript.DynamicItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			if (usedSlot == null || usedSlot.ItemObject == null) return;

			var hasConstructionMenu = usedSlot.ItemObject.GetComponent<BuildingMaterial>();
			if (hasConstructionMenu == null) return;

			var entry = hasConstructionMenu.BuildList.Entries.ToArray()[msg.EntryIndex];

			if (!entry.CanBuildWith(hasConstructionMenu)) return;

			//check if the space to construct on is passable
			if (!MatrixManager.IsPassableAtAllMatricesOneTile((Vector3Int) playerObject.TileWorldPosition(), true, includingPlayers: false))
			{
				Chat.AddExamineMsg(playerObject, "It won't fit here.");
				return;
			}

			//if we are building something impassable, check if there is anything on the space other than the performer.
			var atPosition =
				MatrixManager.GetAt<RegisterTile>((Vector3Int) playerObject.TileWorldPosition(), true);

			if (entry.Prefab == null)
			{
				//requires immediate attention, show it regardless of log filter:
				Logger.Log($"Construction entry is missing prefab for {entry.Name}", Category.Construction);
				return;
			}

			var registerTile = entry.Prefab.GetComponent<RegisterTile>();
			if (registerTile == null)
			{
				Logger.LogWarningFormat("Buildable prefab {0} has no registerTile, no idea if it's passable", Category.Construction, entry.Prefab);
			}
			var builtObjectIsImpassable = registerTile == null || !registerTile.IsPassable(true);
			foreach (var thingAtPosition in atPosition)
			{
				if (entry.OnePerTile)
				{
					//can only build one of this on a given tile
					if (entry.Prefab.Equals(Spawn.DeterminePrefab(thingAtPosition.gameObject)))
					{
						Chat.AddExamineMsg(playerObject, $"There's already one here.");
						return;
					}
				}

				if (builtObjectIsImpassable)
				{
					//if the object we are building is itself impassable, we should check if anything blocks construciton.
					//otherwise it's fine to add it to the pile on the tile
					if (ServerValidations.IsConstructionBlocked(playerObject, null,
						playerObject.TileWorldPosition())) return;
				}
			}

			//build and consume
			void ProgressComplete()
			{
				var builtObject =
					entry.ServerBuild(SpawnDestination.At(playerScript.RegisterPlayer), hasConstructionMenu);

				if(builtObject == null) return;

				Chat.AddActionMsgToChat(playerObject, $"You finish building the {entry.Name}.",
					$"{playerObject.ExpensiveName()} finishes building the {entry.Name}.");

				if(entry.FacePlayerDirectionOnConstruction == false) return;
				if (builtObject.TryGetComponent<Rotatable>(out var rotatable) == false) return;
				if (playerScript.TryGetComponent<Rotatable>(out var playerRotatable) == false) return;

				//Face players direction
				rotatable.FaceDirection(playerRotatable.CurrentDirection);
			}

			Chat.AddActionMsgToChat(playerObject, $"You begin building the {entry.Name}...",
				$"{playerObject.ExpensiveName()} begins building the {entry.Name}...");
			ToolUtils.ServerUseTool(playerObject, usedSlot.ItemObject,
				ActionTarget.Tile(playerScript.RegisterPlayer.WorldPositionServer), entry.BuildTime,
				ProgressComplete);
		}

		/// <summary>
		/// Request constructing the given entry
		/// </summary>
		/// <param name="entry">entry to build</param>
		/// <param name="hasMenu">has construction menu component of the object being used to
		/// construct.</param>
		/// <returns></returns>
		public static NetMessage Send(BuildList.Entry entry, BuildingMaterial hasMenu)
		{
			int entryIndex = hasMenu.BuildList.Entries.ToList().IndexOf(entry);
			if (entryIndex == -1) return new NetMessage(); // entryIndex was previously a byte, which made this check impossible.

			NetMessage msg = new NetMessage
			{
				EntryIndex = (byte) entryIndex
			};

			Send(msg);
			return msg;
		}
	}
}
