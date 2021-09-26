using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Messages.Server;
using Systems.Interaction;


namespace Messages.Client
{
	/// <summary>
	///     Informs server of interaction with some object's tab element
	/// </summary>
	public class TabInteractMessage : ClientMessage<TabInteractMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint TabProvider;
			public NetTabType NetTabType;
			public string ElementId;

			public byte[] ElementValue;
		}

		//Serverside
		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.TabProvider);
			ProcessFurther(SentByPlayer, NetworkObject, msg);
		}

		private void ProcessFurther(ConnectedPlayer player, GameObject tabProvider, NetMessage msg)
		{
			if (player == null)
			{
				Logger.LogWarning("[TabInteractMessage.ProcessFurther] - player is null", Category.NetUI);
				return;
			}
			else if (tabProvider == null)
			{
				Logger.LogWarning("[TabInteractMessage.ProcessFurther] - tabProvider is null", Category.NetUI);
				return;
			}

			var playerScript = player.Script;

			//First Validations is for objects in the world (computers, etc), second check is for items in active hand (null rod, PADs).
			bool validate;
			if (playerScript.PlayerState == PlayerScript.PlayerStates.Ai)
			{
				validate = Validations.CanApply(new AiActivate(player.GameObject, null,
					tabProvider, Intent.Help, AiActivate.ClickTypes.NormalClick), NetworkSide.Server);
			}
			else
			{
				try
				{
					validate = Validations.CanApply(player.Script, tabProvider, NetworkSide.Server)
					           || playerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject == tabProvider;
				}
				catch (NullReferenceException exception)
				{
					Logger.LogError("Caught NRE in TabInterectMessage.Process: " + exception.Message, Category.Interaction);
					return;
				}
			}

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

		private TabUpdateMessage FailValidation(ConnectedPlayer player, GameObject tabProvider, NetMessage msg, string reason = "")
		{
			Logger.LogWarning($"{player.Name}: Tab interaction w/{tabProvider} denied: {reason}", Category.NetUI);
			return TabUpdateMessage.Send(player.GameObject, tabProvider, msg.NetTabType, TabAction.Close);
		}

		public static NetMessage Send(
			GameObject tabProvider,
			NetTabType netTabType,
			string elementId,
			byte[] elementValue = null)
		{

			NetMessage msg = new NetMessage
			{
				TabProvider = tabProvider.NetId(),
				NetTabType = netTabType,
				ElementId = elementId,
				ElementValue = elementValue
			};

			Send(msg);
			return msg;
		}
	}
}
