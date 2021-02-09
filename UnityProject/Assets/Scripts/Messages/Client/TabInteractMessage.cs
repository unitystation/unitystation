using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

/// <summary>
///     Informs server of interaction with some object's tab element
/// </summary>
public class TabInteractMessage : ClientMessage
{
	public uint TabProvider;
	public NetTabType NetTabType;
	public string ElementId;

	public byte[] ElementValue;

	//Serverside
	public override void Process()
	{
//		Logger.Log("Processed " + ToString());
		LoadNetworkObject(TabProvider);
		ProcessFurther(SentByPlayer, NetworkObject);
	}

	private void ProcessFurther(ConnectedPlayer player, GameObject tabProvider)
	{
		if (player == null)
		{
			Logger.LogWarning("[TabInteractMessage.ProcessFurther] - player is null");
			return;
		}
		else if (tabProvider == null)
		{
			Logger.LogWarning("[TabInteractMessage.ProcessFurther] - tabProvider is null");
			return;
		}

		var playerScript = player.Script;
		//First Validations is for objects in the world (computers, etc), second check is for items in active hand (null rod, PADs).
		bool validate = Validations.CanApply(player.Script, tabProvider, NetworkSide.Server)
		                || playerScript.ItemStorage.GetActiveHandSlot().ItemObject == tabProvider;
		if (!validate)
		{
			FailValidation(player, tabProvider, "Can't interact/reach");
			return;
		}

		var tabInfo = NetworkTabManager.Instance.Get(tabProvider, NetTabType);
		if (!tabInfo /* == NetworkTabInfo.Invalid*/)
		{
			//No such tab exists on server!
			FailValidation(player, tabProvider, $"No such tab: {tabProvider}/{NetTabType}");
			return;
		}

		var updatedElement = tabInfo[ElementId];
		if (updatedElement == null)
		{
			//No such element exists on server!
			FailValidation(player, tabProvider, $"No such element: {tabInfo}[{ElementId}]");
			return;
		}

		if (updatedElement.InteractionMode == ElementMode.ServerWrite)
		{
			//Don't change labels and other non-interactable elements. If this is triggered, someone's tampering with client
			FailValidation(player, tabProvider, $"Non-interactable {updatedElement}");
			return;
		}

		var valueBeforeUpdate = updatedElement.ValueObject;
		updatedElement.BinaryValue = ElementValue;
		updatedElement.ExecuteServer(player);

		if (updatedElement.InteractionMode == ElementMode.ClientWrite)
		{
			//Don't rememeber value provided by client and restore to the initial one
			updatedElement.ValueObject = valueBeforeUpdate;
		}

		//Notify all peeping players of the change
		List<ConnectedPlayer> list = NetworkTabManager.Instance.GetPeepers(tabProvider, NetTabType);
		for (var i = 0; i < list.Count; i++)
		{
			var connectedPlayer = list[i];
//Not sending that update to the same player
			if (connectedPlayer.GameObject != player.GameObject)
			{
				TabUpdateMessage.Send(connectedPlayer.GameObject, tabProvider, NetTabType, TabAction.Update,
					player.GameObject,
					new[] {new ElementValue {Id = ElementId, Value = updatedElement.BinaryValue}});
			}
		}
	}

	private TabUpdateMessage FailValidation(ConnectedPlayer player, GameObject tabProvider, string reason = "")
	{
		Logger.LogWarning($"{player.Name}: Tab interaction w/{tabProvider} denied: {reason}", Category.NetUI);
		return TabUpdateMessage.Send(player.GameObject, tabProvider, NetTabType, TabAction.Close);
	}

	public static TabInteractMessage Send(
		GameObject tabProvider,
		NetTabType netTabType,
		string elementId,
		byte[] elementValue = null)
	{

		TabInteractMessage msg = new TabInteractMessage
		{
			TabProvider = tabProvider.NetId(),
			NetTabType = netTabType,
			ElementId = elementId,
			ElementValue = elementValue
		};
		msg.Send();
		return msg;
	}
}
