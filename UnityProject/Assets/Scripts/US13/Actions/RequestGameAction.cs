using US13.HealthV2.Living;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using UnityEngine;
using US13.Managers;
using US13.Messages.Client;

namespace US13.Actions
{
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
			if (!componentIDToComponentType.TryGetValue(msg.ComponentID, out var type)) return;
			LoadNetworkObject(msg.NetObject);

			if (SentByPlayer == PlayerInfo.Invalid) return;

			var IActionGUIs = ActionSource.GetActions(NetworkObject, type);
			if (msg.ComponentLocation >= 0 && IActionGUIs.Length > msg.ComponentLocation)
			{
				if (IActionGUIs[msg.ComponentLocation] is IServerActionGUI IServerActionGUI)
				{
					IServerActionGUI.CallActionServer(SentByPlayer);
					return;
				}

				if (IActionGUIs[msg.ComponentLocation] is IServerActionGUIMulti IServerActionGUIMulti)
				{
					if (msg.listIndex >= 0 && msg.listIndex < IServerActionGUIMulti.ActionData.Count)
						IServerActionGUIMulti.CallActionServer(IServerActionGUIMulti.ActionData[msg.listIndex], SentByPlayer);
				}
			}
		}

		public static void Send(IActionGUI iServerActionGUI)
		{
			if (iServerActionGUI is Component or IBodyPartBehavior)
			{
				SendToComponent(iServerActionGUI);
			}
			//else not doing anything, implying custom sending
		}

		private static void SendToComponent(IActionGUI actionComponent)
		{
			var netObject = ActionSource.GetNetworkIdentity(actionComponent);
			var componentType = actionComponent.GetType();
			var childActions = ActionSource.GetActions(netObject == null ? null : netObject.gameObject, componentType);
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

			Loggy.Error("Failed to find IServerActionGUI on NetworkIdentity", Category.UserInput);
		}

		public static void Send(IActionGUIMulti iServerActionGUIMulti, ActionData action)
		{
			if (iServerActionGUIMulti is Component or IBodyPartBehavior)
			{
				SendToComponent(iServerActionGUIMulti, action);
			}
			//else not doing anything, implying custom sending
		}

		private static void SendToComponent(IActionGUIMulti actionComponent, ActionData actionChosen)
		{
			var netObject = ActionSource.GetNetworkIdentity(actionComponent);
			var componentType = actionComponent.GetType();
			var childActions = ActionSource.GetActions(netObject == null ? null : netObject.gameObject, componentType);
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

			Loggy.Error("Failed to find IServerActionGUI on NetworkIdentity", Category.UserInput);
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
	/// <summary>Resolve the network owner and action instances for both components and managed behaviors.</summary>
	public static class ActionSource
	{
		public static NetworkIdentity GetNetworkIdentity(IAction action) => action switch
		{
			BodyPartBehavior behavior => behavior.netIdentity,
			Component component => component.GetComponentInParent<NetworkIdentity>(),
			_ => null
		};

		public static IAction[] GetActions(GameObject root, Type type)
		{
			if (root == null) return Array.Empty<IAction>();
			var native = typeof(Component).IsAssignableFrom(type) || type.IsInterface
				? root.GetComponentsInChildren(type).Cast<object>()
				: Enumerable.Empty<object>();
			var managed = root.GetComponentsInChildren<BodyPartBehaviors>()
				.SelectMany(host => host.Behaviors).Where(type.IsInstanceOfType).Cast<object>();
			return native.Concat(managed).OfType<IAction>().ToArray();
		}
	}

}
