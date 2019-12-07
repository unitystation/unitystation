
using System.Collections;
using System.Linq;
using Construction;

/// <summary>
/// Client requests to construct something using the material in their active hand.
/// </summary>
public class RequestConstructionMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestConstructionMessage;

	//index of the entry in the ConstructionList.
	public byte EntryIndex;

	public override IEnumerator Process()
	{
		var clientStorage = SentByPlayer.Script.ItemStorage;
		var usedSlot = clientStorage.GetActiveHandSlot();
		if (usedSlot == null || usedSlot.ItemObject == null) yield break;

		var hasConstructionMenu = usedSlot.ItemObject.GetComponent<BuildingMaterial>();
		if (hasConstructionMenu == null) yield break;

		var entry = hasConstructionMenu.BuildList.Entries.ToArray()[EntryIndex];

		if (!entry.CanBuildWith(hasConstructionMenu)) yield break;

		//build and consume
		var finishProgressAction = new ProgressCompleteAction(() =>
		{
			if (entry.ServerBuild(SpawnDestination.At(SentByPlayer.Script.registerTile), hasConstructionMenu))
			{
				Chat.AddActionMsgToChat(SentByPlayer.GameObject, $"You finish building the {entry.Name}.",
					$"{SentByPlayer.GameObject.ExpensiveName()} finishes building the {entry.Name}.");
			}
		});

		Chat.AddActionMsgToChat(SentByPlayer.GameObject, $"You begin building the {entry.Name}...",
			$"{SentByPlayer.GameObject.ExpensiveName()} begins building the {entry.Name}...");
		ToolUtils.ServerUseTool(SentByPlayer.GameObject, usedSlot.ItemObject,
			SentByPlayer.Script.registerTile.WorldPositionServer.To2Int(), entry.BuildTime,
			finishProgressAction);
	}

	/// <summary>
	/// Request constructing the given entry
	/// </summary>
	/// <param name="entry">entry to build</param>
	/// <param name="hasMenu">has construction menu component of the object being used to
	/// construct.</param>
	/// <returns></returns>
	public static RequestConstructionMessage Send(BuildList.Entry entry, BuildingMaterial hasMenu)
	{
		byte entryIndex = (byte) hasMenu.BuildList.Entries.ToList().IndexOf(entry);
		if (entryIndex == -1) return null;

		RequestConstructionMessage msg = new RequestConstructionMessage
		{
			EntryIndex = entryIndex
		};
		msg.Send();
		return msg;
	}
}
