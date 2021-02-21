using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

/// <summary>
///     Informs server of interaction with some object's tab element
/// </summary>
public class TabInteractMessage : ClientMessage
{
	public struct TabInteractMessageNetMessage : NetworkMessage
	{
		public uint TabProvider;
		public NetTabType NetTabType;
		public string ElementId;

		public byte[] ElementValue;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public TabInteractMessageNetMessage IgnoreMe;

	//Serverside
	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as TabInteractMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.TabProvider);
		ProcessFurther(SentByPlayer, NetworkObject, newMsg);
	}

	private void ProcessFurther(ConnectedPlayer player, GameObject tabProvider, TabInteractMessageNetMessage msg)
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
			FailValidation(player, tabProvider, msg,"Can't interact/reach");
			return;
		}

		var tabInfo = NetworkTabManager.Instance.Get(tabProvider, msg.NetTabType);
		if (!tabInfo /* == NetworkTabInfo.Invalid*/)
		{
			//No such tab exists on server!
			FailValidation(player, tabProvider, msg,$"No such tab: {tabProvider}/{msg.NetTabType}");
			return;
		}

		var updatedElement = tabInfo[msg.ElementId];
		if (updatedElement == null)
		{
			//No such element exists on server!
			FailValidation(player, tabProvider, msg,$"No such element: {tabInfo}[{msg.ElementId}]");
			return;
		}

		if (updatedElement.InteractionMode == ElementMode.ServerWrite)
		{
			//Don't change labels and other non-interactable elements. If this is triggered, someone's tampering with client
			FailValidation(player, tabProvider, msg,$"Non-interactable {updatedElement}");
			return;
		}

		var valueBeforeUpdate = updatedElement.ValueObject;
		updatedElement.BinaryValue = msg.ElementValue;
		updatedElement.ExecuteServer(player);

		if (updatedElement.InteractionMode == ElementMode.ClientWrite)
		{
			//Don't rememeber value provided by client and restore to the initial one
			updatedElement.ValueObject = valueBeforeUpdate;
		}

		//Notify all peeping players of the change
		List<ConnectedPlayer> list = NetworkTabManager.Instance.GetPeepers(tabProvider, msg.NetTabType);
		for (var i = 0; i < list.Count; i++)
		{
			var connectedPlayer = list[i];
//Not sending that update to the same player
			if (connectedPlayer.GameObject != player.GameObject)
			{
				TabUpdateMessage.Send(connectedPlayer.GameObject, tabProvider, msg.NetTabType, TabAction.Update,
					player.GameObject,
					new[] {new ElementValue {Id = msg.ElementId, Value = updatedElement.BinaryValue}});
			}
		}
	}

	private TabUpdateMessage FailValidation(ConnectedPlayer player, GameObject tabProvider, TabInteractMessageNetMessage msg, string reason = "")
	{
		Logger.LogWarning($"{player.Name}: Tab interaction w/{tabProvider} denied: {reason}", Category.NetUI);
		return TabUpdateMessage.Send(player.GameObject, tabProvider, msg.NetTabType, TabAction.Close);
	}

	public static TabInteractMessageNetMessage Send(
		GameObject tabProvider,
		NetTabType netTabType,
		string elementId,
		byte[] elementValue = null)
	{

		TabInteractMessageNetMessage msg = new TabInteractMessageNetMessage
		{
			TabProvider = tabProvider.NetId(),
			NetTabType = netTabType,
			ElementId = elementId,
			ElementValue = elementValue
		};
		new TabInteractMessage().Send(msg);
		return msg;
	}
}
