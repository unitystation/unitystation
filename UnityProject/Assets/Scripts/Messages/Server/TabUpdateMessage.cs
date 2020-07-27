using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TabUpdateMessage : ServerMessage
{
	public uint Provider;
	public NetTabType Type;
	public TabAction Action;

	public ElementValue[] ElementValues;

	public bool Touched;

	private static readonly ElementValue[] NoValues = new ElementValue[0];

	public override void Process()
	{
		Logger.LogTraceFormat("Processed {0}", Category.NetUI, this);
		LoadNetworkObject(Provider);
		switch (Action)
		{
			case TabAction.Open:
				ControlTabs.ShowTab(Type, NetworkObject, ElementValues);
				break;
			case TabAction.Close:
				ControlTabs.CloseTab(Type, NetworkObject);
				break;
			case TabAction.Update:
				ControlTabs.UpdateTab(Type, NetworkObject, ElementValues, Touched);
				break;
		}
	}

	public override string ToString()
	{
		return $"[TabUpdateMessage {nameof(Provider)}: {Provider}, {nameof(Type)}: {Type}, " +
		       $"{nameof(Action)}: {Action}, " +
		       $"{nameof(ElementValue)}: {string.Join("; ", ElementValues ?? NoValues)}]";
	}

	public static void SendToPeepers(GameObject provider, NetTabType type, TabAction tabAction, ElementValue[] values = null)
	{
		//Notify all peeping players of the change
		var list = NetworkTabManager.Instance.GetPeepers(provider, type);
		foreach (var connectedPlayer in list)
		{
			Send(connectedPlayer.GameObject, provider, type, tabAction, null, values);
		}
	}

	public static TabUpdateMessage Send(GameObject recipient, GameObject provider, NetTabType type, TabAction tabAction,
		GameObject changedBy = null,
		ElementValue[] values = null)
	{

		if (values != null)
		{
			// get max possible packet size from current transform
			int maxPacketSize = Transport.activeTransport.GetMaxPacketSize(0);
			// set currentSize start value to max TCP header size (60b)
			int currentSize = 60;

			List<ElementValue> elementValues = new List<ElementValue>();

			foreach (var value in values)
			{
				currentSize += value.GetSize();

				if (currentSize > maxPacketSize)
				{
					Logger.LogError("[TabUpdateMessage.Send] - message is to big to send in one packet", Category.NetMessage);
					break;
				}

				elementValues.Add(value);
			}

			values = elementValues.ToArray();
		}


		var msg = new TabUpdateMessage
		{
			Provider = provider.NetId(),
			Type = type,
			Action = tabAction,
			ElementValues = values,
			Touched = changedBy != null
		};
		switch (tabAction)
		{
			case TabAction.Open:
				NetworkTabManager.Instance.Add(provider, type, recipient);
				//!! resetting ElementValues
				msg.ElementValues = NetworkTabManager.Instance.Get(provider, type).ElementValues;
				//!!
				break;
			case TabAction.Close:
				NetworkTabManager.Instance.Remove(provider, type, recipient);
				break;
			case TabAction.Update:

				//fixme: duplication of NetTab.ValidatePeepers
				//Not sending updates and closing tab for players that don't pass the validation anymore
				var validate = Validations.CanApply(recipient, provider, NetworkSide.Server);
				if (!validate)
				{
					Send(recipient, provider, type, TabAction.Close);
					return msg;
				}

				break;
		}

		msg.SendTo(recipient);
		Logger.LogTraceFormat("{0}", Category.NetUI, msg);
		return msg;
	}
}

public struct ElementValue
{
	public string Id;
	public byte[] Value;

	public override string ToString()
	{
		return $"[{Id}={Value}]";
	}

	/// <summary>
	/// Get size of this object (in bytes)
	/// </summary>
	/// <returns>size of this object (in bytes)</returns>
	public int GetSize()
	{
		return sizeof(char) * Id.Length		// Id
			+ sizeof(byte) * Value.Length;	// Value
	}
}

public enum TabAction
{
	Open,
	Close,
	Update
}