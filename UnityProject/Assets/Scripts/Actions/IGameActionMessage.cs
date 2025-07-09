using Mirror;
using UnityEngine;

//The netmessage naming is:
//Client -> server: Are called requests, because the client is 'requesting' a change that the server must then validate
//Server -> client: Are called updates, because they are information the client should generally always make use of
//                  the server does not usually expect a response back from updates
//I wrote this at 2am while sleep deprived this needs rewording
namespace GameActions
{
	public interface IGameActionNetworkMessage : NetworkMessage
	{
		/// <summary>
		/// The UUID of the action receiving us
		/// </summary>
		public string ReceivingActionGuid { get; set; }
		public string SentSerializedData { get; set; }
	}

	public interface IGameActionRequestMessage : IGameActionNetworkMessage
	{
		/// <summary>
		/// Should the requested action attempt to be triggered(usually yes)
		/// </summary>
		public bool AttemptTrigger { get; set; }
	}
}

