using System.Collections.Generic;
using Hacking;
using Messages.Server;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	//Give me admin piz
	public class RequestHackingInteraction : ClientMessage<RequestHackingInteraction.NetMessage>
	{
		public enum InteractionWith
		{
			Cable,
			RemoteSignaller, //Probably should be generic but can't think of much stuff to add, If you want you can make it generic if you are adding as a stuff
			Bomb,
			CutWire
		}

		public struct NetMessage : NetworkMessage
		{
			public uint netIDOfObjectBeingHacked;
			public InteractionWith InteractionType;
			public int PanelInputID;
			public int PanelOutputID;
			public uint NetIDOfInteractionObject;
			public string ExtraData;
		}

		public override void Process(NetMessage msg)
		{

			LoadMultipleObjects(new []{msg.netIDOfObjectBeingHacked, msg.NetIDOfInteractionObject});
			var HackingProcessBase = NetworkObjects[0].GetComponent<HackingProcessBase>();
			HackingProcessBase.ProcessCustomInteraction(SentByPlayer.GameObject,msg.InteractionType, NetworkObjects[1],msg.PanelInputID,  msg.PanelOutputID);
		}

		public static NetMessage Send(GameObject hackObject,uint InNetIDOfInteractionObject, int InPanelInputID, int InPanelOutputID, InteractionWith InInteractionType )
		{
			NetMessage msg = new NetMessage
			{
				netIDOfObjectBeingHacked = hackObject.GetComponent<NetworkIdentity>().netId,
				InteractionType = InInteractionType,
				NetIDOfInteractionObject = InNetIDOfInteractionObject,
				PanelInputID = InPanelInputID,
				PanelOutputID  =InPanelOutputID
			};

			Send(msg);
			return msg;
		}
	}
}
