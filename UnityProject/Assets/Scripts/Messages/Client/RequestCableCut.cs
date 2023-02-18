using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestCableCut : ClientMessage<RequestCableCut.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public Vector3 targetWorldPosition;
		public string Name;
		public TileType TileType;
	}

	public override void Process(NetMessage msg)
	{
		if (Validations.CanInteract(SentByPlayer.Script, NetworkSide.Server)  == false) return;
		if (Validations.IsReachableByPositions(SentByPlayer.Script.gameObject.AssumedWorldPosServer(), msg.targetWorldPosition  , true ) == false) return;

		InteractableTiles.ServerPerformCableCuttingInteraction(SentByPlayer.Connection, msg,
			SentByPlayer.Script.gameObject);
	}

	public static void Send(Vector3 targetWorldPosition, string Name, TileType TileType)
	{

		var Net = new NetMessage()
		{
			targetWorldPosition = targetWorldPosition,
			Name = Name,
			TileType = TileType
		};


		Send(Net);
	}

}
