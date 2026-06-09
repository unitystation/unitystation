using System.Linq;
using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Lifecycle;
using US13.Items.Tool;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Objects.Cargo.ConveyorBelts;
using US13.Systems.Construction;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Systems;
using Util;

namespace US13.Messages.Client
{
	/// <summary>
	/// Client requests to construct a conveyor belt using the material in their active hand.
	/// Yes its a big old cut and paste from RequestBuildMessage
	/// </summary>
	public class RequestConveyorBuildMessage : ClientMessage<RequestConveyorBuildMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			//index of the entry in the ConstructionList.
			public byte EntryIndex;
			public ConveyorBelt.ConveyorDirection Direction;
			public bool Sandboxing;
		}

		public override void Process(NetMessage msg)
		{
			var playerScript = SentByPlayer.Script;
			if (msg.Sandboxing && AdminCommandsManager.HasPermission(playerScript.connectionToClient, out var _, "MAP_SPAWN"))
			{
				var spawnedObj = Spawn.ServerPrefab(UIManager.BuildMenu.ConveyorBuildMenu.ConveyorBeltPrefab.Prefab, playerScript.RegisterPlayer.WorldPosition)?.GameObject;
				if (spawnedObj)
				{
					var conveyorBelt = spawnedObj.GetComponent<ConveyorBelt>();
					if (conveyorBelt != null) conveyorBelt.SetBeltFromBuildMenu(msg.Direction);
				}
				return;
			}

			var playerObject = SentByPlayer.GameObject;
			var clientStorage = playerScript.DynamicItemStorage;
			var usedSlot = clientStorage.GetActiveHandSlot();
			if (usedSlot == null || usedSlot.ItemObject == null) return;
			var hasConstructionMenu = usedSlot.ItemObject.GetComponent<BuildingMaterial>();
			if (hasConstructionMenu == null) return;

			var entry = hasConstructionMenu.BuildList.Entries.ToArray()[msg.EntryIndex];

			if (!entry.CanBuildWith(hasConstructionMenu)) return;

			//check if the space to construct on is passable
			if (!MatrixManager.IsPassableAtAllMatricesOneTile((Vector3Int)playerObject.TileWorldPosition(), true, includingPlayers: false))
			{
				Chat.AddExamineMsg(playerObject, "It won't fit here.");
				return;
			}

			//if we are building something impassable, check if there is anything on the space other than the performer.
			var atPosition =
				MatrixManager.GetAt<RegisterTile>((Vector3Int)playerObject.TileWorldPosition(), true);

			if (entry.Prefab == null)
			{
				//requires immediate attention, show it regardless of log filter:
				Loggy.Info($"Construction entry is missing prefab for {entry.Name}", Category.Construction);
				return;
			}

			var registerTile = entry.Prefab.GetComponent<RegisterTile>();
			if (registerTile == null)
			{
				Loggy.Warning().Format("Buildable prefab {0} has no registerTile, no idea if it's passable", Category.Construction, entry.Prefab);
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
				var spawnedObj = entry.ServerBuild(SpawnDestination.At(playerScript.RegisterPlayer), hasConstructionMenu);
				if (spawnedObj)
				{
					var conveyorBelt = spawnedObj.GetComponent<ConveyorBelt>();
					if (conveyorBelt != null) conveyorBelt.SetBeltFromBuildMenu(msg.Direction);

					Chat.AddActionMsgToChat(playerObject, $"You finish building the {entry.Name}.",
						 $"{playerObject.ExpensiveName()} finishes building the {entry.Name}.");
				}
			}
			Chat.AddActionMsgToChat(playerObject, $"You begin building the {entry.Name}...",
				$"{playerObject.ExpensiveName()} begins building the {entry.Name}...");
			ToolUtils.ServerUseTool(playerObject, usedSlot.ItemObject,
				ActionTarget.Tile(playerScript.RegisterPlayer.WorldPositionServer), entry.BuildTime,
				ProgressComplete);
		}

		public static NetMessage Send(BuildList.Entry entry, BuildingMaterial hasMenu,
			ConveyorBelt.ConveyorDirection direction, bool isSandbox)
		{
			if (isSandbox)
			{
				var smsg = new NetMessage
				{
					Direction = direction,
					Sandboxing = isSandbox,
				};
				Send(smsg);
				return smsg;
			}
			var entryIndex = hasMenu.BuildList.Entries.ToList().IndexOf(entry);
			if (entryIndex == -1) return new NetMessage();

			var msg = new NetMessage
			{
				EntryIndex = (byte)entryIndex,
				Direction = direction,
				Sandboxing = isSandbox,
			};

			Send(msg);
			return msg;
		}
	}
}
