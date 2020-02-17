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
	public static short MessageType = (short)MessageTypes.RequestGameAction;

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








	public override IEnumerator Process()
	{
		yield return WaitFor(NetObject);
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


	public static RequestGameAction Send(IServerActionGUI iServerActionGUI )
	{
		var netObject = (iServerActionGUI as Component).GetComponent<NetworkIdentity>();
		var _ComponentType = iServerActionGUI.GetType();
		var iServerActionGUIs = netObject.GetComponentsInChildren(_ComponentType);
		var _ComponentLocation = 0;
		bool Found = false;
		foreach (var _iServerActionGUI in iServerActionGUIs) {
			if ((_iServerActionGUI as IServerActionGUI) == iServerActionGUI) {
				Found = true;
				break;
			}
			_ComponentLocation++;
		}
		if (Found)
		{
			RequestGameAction msg = new RequestGameAction
			{
				NetObject = netObject.netId,
				ComponentLocation = _ComponentLocation,
				ComponentType =	_ComponentType,
			};
			msg.Send();
			return msg;
		}
		else {

			Logger.LogError("Failed to find IServerActionGUI on NetworkIdentity");
		}
		return (null);
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
