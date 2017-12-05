using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

public abstract class ClientMessage<T> : GameMessage<T>
{
    public NetworkInstanceId SentBy;

    public void Send()
    {
        SentBy = LocalPlayerId();
        CustomNetworkManager.Instance.client.connection.Send(MessageType, this);
        //		Debug.LogFormat("Sent {0}", this);
    }

    public void SendUnreliable()
    {
        SentBy = LocalPlayerId();
        CustomNetworkManager.Instance.client.connection.SendUnreliable(MessageType, this);
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

}
