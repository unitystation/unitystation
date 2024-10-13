using System.Collections;
using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Messages.Client;

public class RequestGameAction : ClientMessage<RequestGameAction.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public int ComponentLocation;
		public uint NetObject;
		public ushort ComponentID;
		public short listIndex;
	}

	public static readonly Dictionary<ushort, Type> componentIDToComponentType = new Dictionary<ushort, Type>(); //These are useful
	public static readonly Dictionary<Type, ushort> componentTypeToComponentID = new Dictionary<Type, ushort>();

	static RequestGameAction()
	{
		//initialize id mappings
		var alphabeticalComponentTypes =
			typeof(IAction).Assembly.GetTypes()
				.Where(type => typeof(IAction).IsAssignableFrom(type))
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

		if (SentByPlayer == PlayerInfo.Invalid) return;

		var IActionGUIs = NetworkObject.GetComponentsInChildren(type);
		if (IActionGUIs.Length > msg.ComponentLocation)
		{
			if (IActionGUIs[msg.ComponentLocation] is IServerActionGUI IServerActionGUI)
			{
				IServerActionGUI.CallActionServer(SentByPlayer);
				return;
			}

			if (IActionGUIs[msg.ComponentLocation] is IServerActionGUIMulti IServerActionGUIMulti)
			{
				IServerActionGUIMulti.CallActionServer(IServerActionGUIMulti.ActionData[msg.listIndex], SentByPlayer);
			}
		}
	}

	public static void Send(IActionGUI iServerActionGUI)
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

		Loggy.LogError("Failed to find IServerActionGUI on NetworkIdentity", Category.UserInput);
	}

	public static void Send(IActionGUIMulti iServerActionGUIMulti, ActionData action)
	{
		if (iServerActionGUIMulti is Component)
		{
			SendToComponent(iServerActionGUIMulti, action);
		}
		//else not doing anything, implying custom sending
	}

	private static void SendToComponent(IActionGUIMulti actionComponent, ActionData actionChosen)
	{
		var netObject = ((Component) actionComponent).GetComponent<NetworkIdentity>();
		var componentType = actionComponent.GetType();
		var childActions = netObject.GetComponentsInChildren(componentType);
		int componentLocation = 0;
		bool found = false;

		foreach (var action in childActions)
		{
			if ((action as IServerActionGUIMulti) == actionComponent)
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
				listIndex = FindIndex(actionComponent, actionChosen)
			};
			Send(msg);
			return;
		}

		Loggy.LogError("Failed to find IServerActionGUI on NetworkIdentity", Category.UserInput);
	}

	public static short FindIndex(IActionGUIMulti actionComponent, ActionData actionChosen)
	{
		for (int i = 0; i < actionComponent.ActionData.Count; i++)
		{
			if(actionComponent.ActionData[i] != actionChosen) continue;

			return (short)i;
		}

		return 0;
	}
}
