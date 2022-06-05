using System.Collections;
using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Messages.Client;

public class RequestGameAction : ClientMessage<RequestGameAction.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public int ComponentLocation;
		public uint NetObject;
		public ushort ComponentID;
	}

	public static readonly Dictionary<ushort, Type> componentIDToComponentType = new Dictionary<ushort, Type>(); //These are useful
	public static readonly Dictionary<Type, ushort> componentTypeToComponentID = new Dictionary<Type, ushort>();

	static RequestGameAction()
	{
		//initialize id mappings
		var alphabeticalComponentTypes =
			typeof(IActionGUI).Assembly.GetTypes()
				.Where(type => typeof(IActionGUI).IsAssignableFrom(type))
				.OrderBy(type => type.FullName);
		ushort i = 0;
		foreach (var componentType in alphabeticalComponentTypes)
		{
			componentIDToComponentType.Add(i, componentType);
			componentTypeToComponentID.Add(componentType, i);
			i++;
		}

	}

	public override void Process(NetMessage msg)
	{
		var type = componentIDToComponentType[msg.ComponentID];
		LoadNetworkObject(msg.NetObject);

		if (SentByPlayer != PlayerInfo.Invalid)
		{
			var IActionGUIs = NetworkObject.GetComponentsInChildren(type);
			if (IActionGUIs.Length > msg.ComponentLocation)
			{
				var IServerActionGUI = IActionGUIs[msg.ComponentLocation] as IServerActionGUI;
				IServerActionGUI.CallActionServer(SentByPlayer);
			}
		}
	}

	public static void Send(IActionGUI iServerActionGUI )
	{
		if (iServerActionGUI is Component)
		{
			SendToComponent(iServerActionGUI);
		}
		//else not doing anything, implying custom sending
	}

	private static void SendToComponent(IActionGUI actionComponent)
	{
		var netObject = ((Component) actionComponent).GetComponent<NetworkIdentity>();
		var componentType = actionComponent.GetType();
		var childActions = netObject.GetComponentsInChildren(componentType);
		int componentLocation = 0;
		bool found = false;
		foreach (var action in childActions)
		{
			if ((action as IServerActionGUI) == actionComponent)
			{
				found = true;
				break;
			}
			componentLocation++;
		}
		if (found)
		{
			var msg = new NetMessage
			{
				NetObject = netObject.netId,
				ComponentLocation = componentLocation,
				ComponentID = componentTypeToComponentID[componentType],
			};
			Send(msg);
			return;
		}

		Logger.LogError("Failed to find IServerActionGUI on NetworkIdentity", Category.UserInput);
	}
}
