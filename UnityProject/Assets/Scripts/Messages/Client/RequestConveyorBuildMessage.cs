
using System.Collections;
using System.Linq;
using Construction;
using UnityEngine;

/// <summary>
/// Client requests to construct a conveyor belt using the material in their active hand.
/// Yes its a big old cut and paste from RequestBuildMessage
/// </summary>
public class RequestConveyorBuildMessage : ClientMessage
{
	//index of the entry in the ConstructionList.
	public byte EntryIndex;
	public ConveyorBelt.ConveyorDirection Direction;

	public override void Process()
	{
		var clientStorage = SentByPlayer.Script.ItemStorage;
		var usedSlot = clientStorage.GetActiveHandSlot();
		if (usedSlot == null || usedSlot.ItemObject == null) return;

		var hasConstructionMenu = usedSlot.ItemObject.GetComponent<BuildingMaterial>();
		if (hasConstructionMenu == null) return;

		var entry = hasConstructionMenu.BuildList.Entries.ToArray()[EntryIndex];

		if (!entry.CanBuildWith(hasConstructionMenu)) return;

		//check if the space to construct on is passable
		if (!MatrixManager.IsPassableAt((Vector3Int) SentByPlayer.GameObject.TileWorldPosition(), true, includingPlayers: false))
		{
			Chat.AddExamineMsg(SentByPlayer.GameObject, "It won't fit here.");
			return;
		}

		//if we are building something impassable, check if there is anything on the space other than the performer.
		var atPosition =
			MatrixManager.GetAt<RegisterTile>((Vector3Int) SentByPlayer.GameObject.TileWorldPosition(), true);

		if (entry.Prefab == null)
		{
			//requires immediate attention, show it regardless of log filter:
			Logger.Log($"Construction entry is missing prefab for {entry.Name}");
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
					Chat.AddExamineMsg(SentByPlayer.GameObject, $"There's already one here.");
					return;
				}
			}

			if (builtObjectIsImpassable)
			{
				//if the object we are building is itself impassable, we should check if anything blocks construciton.
				//otherwise it's fine to add it to the pile on the tile
				if (ServerValidations.IsConstructionBlocked(SentByPlayer.GameObject, null,
					SentByPlayer.GameObject.TileWorldPosition())) return;
			}
		}

		//build and consume
		void ProgressComplete()
		{
			var spawnedObj = entry.ServerBuild(SpawnDestination.At(SentByPlayer.Script.registerTile), hasConstructionMenu);
			if (spawnedObj)
			{
				var conveyorBelt = spawnedObj.GetComponent<ConveyorBelt>();
				if (conveyorBelt != null) conveyorBelt.SetBeltFromBuildMenu(Direction);

				Chat.AddActionMsgToChat(SentByPlayer.GameObject, $"You finish building the {entry.Name}.",
					$"{SentByPlayer.GameObject.ExpensiveName()} finishes building the {entry.Name}.");
			}
		}

		Chat.AddActionMsgToChat(SentByPlayer.GameObject, $"You begin building the {entry.Name}...",
			$"{SentByPlayer.GameObject.ExpensiveName()} begins building the {entry.Name}...");
		ToolUtils.ServerUseTool(SentByPlayer.GameObject, usedSlot.ItemObject,
			ActionTarget.Tile(SentByPlayer.Script.registerTile.WorldPositionServer), entry.BuildTime,
			ProgressComplete);
	}

	public static RequestConveyorBuildMessage Send(BuildList.Entry entry, BuildingMaterial hasMenu,
		ConveyorBelt.ConveyorDirection direction)
	{
		var entryIndex = hasMenu.BuildList.Entries.ToList().IndexOf(entry);
		if (entryIndex == -1) return null;

		var msg = new RequestConveyorBuildMessage
		{
			EntryIndex = (byte) entryIndex,
			Direction = direction
		};
		msg.Send();
		return msg;
	}
}
