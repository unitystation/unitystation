using System.Reflection;
using PlayGroup;
using UnityEngine.Networking;

public abstract class ClientMessage<T> : GameMessage<T>
{
	public NetworkInstanceId SentBy;

	public void Send()
	{
		SentBy = LocalPlayerId();
		CustomNetworkManager.Instance.client.connection.Send(GetMessageType(), this);
		//		Debug.LogFormat("Sent {0}", this);
	}

	public void SendUnreliable()
	{
		SentBy = LocalPlayerId();
		CustomNetworkManager.Instance.client.connection.SendUnreliable(GetMessageType(), this);
	}

	private static NetworkInstanceId LocalPlayerId()
	{
		return PlayerManager.LocalPlayer.GetComponent<NetworkIdentity>().netId;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		SentBy = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(SentBy);
	}
	
	private short GetMessageType()
	{
		FieldInfo field = typeof(T).GetField("MessageType",
			BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public);
		return (short) field.GetValue(null);
	}
}