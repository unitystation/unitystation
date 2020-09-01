using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class RequestGameAction : ClientMessage
{
	public int ComponentLocation;
	public uint NetObject;
	public Type ComponentType;

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

	public override void Process()
	{
		LoadNetworkObject(NetObject);
		//Logger.Log("ComponentLocation > " + ComponentLocation  + " NetworkObject > " +  NetworkObject + " ComponentType > " + ComponentType);
		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			var IActionGUIs = NetworkObject.GetComponentsInChildren(ComponentType);
			if ((IActionGUIs.Length > ComponentLocation)) {
				var IServerActionGUI = (IActionGUIs[ComponentLocation] as IServerActionGUI);
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
		foreach (var action in childActions) {
			if ((action as IServerActionGUI) == actionComponent) {
				found = true;
				break;
			}
			componentLocation++;
		}
		if (found)
		{
			var msg = new RequestGameAction
			{
				NetObject = netObject.netId,
				ComponentLocation = componentLocation,
				ComponentType =	componentType,
			};
			msg.Send();
			return;
		}

		Logger.LogError("Failed to find IServerActionGUI on NetworkIdentity");
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ComponentType = componentIDToComponentType[reader.ReadUInt16()];
		NetObject = reader.ReadUInt32();
		ComponentLocation = reader.ReadInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt16(componentTypeToComponentID[ComponentType]);
		writer.WriteUInt32(NetObject);
		writer.WriteInt32(ComponentLocation);
	}
}
