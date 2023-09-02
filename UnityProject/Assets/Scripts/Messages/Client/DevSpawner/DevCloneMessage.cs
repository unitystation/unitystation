using Logs;
using Mirror;
using UnityEngine;

namespace Messages.Client.DevSpawner
{
	/// <summary>
	/// Message allowing a client dev / admin to clone something, validated server side.
	/// </summary>
	public class DevCloneMessage : ClientMessage<DevCloneMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// Net ID of the object to clone
			public uint ToClone;
			// position to spawn at.
			public Vector2 WorldPosition;

			public override string ToString()
			{
				return $"[DevCloneMessage ToClone={ToClone} WorldPosition={WorldPosition}]";
			}
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			if (msg.ToClone.Equals(NetId.Invalid))
			{
				Loggy.LogWarning("Attempted to clone an object with invalid netID, clone will not occur.", Category.Admin);
			}
			else
			{
				LoadNetworkObject(msg.ToClone);
				if (MatrixManager.IsPassableAtAllMatricesOneTile(msg.WorldPosition.RoundToInt(), true))
				{
					Spawn.ServerClone(NetworkObject, msg.WorldPosition);
					UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
						$"{SentByPlayer.Username} spawned a clone of {NetworkObject} at {msg.WorldPosition}", SentByPlayer.UserId);
				}
			}
		}

		/// <summary>
		/// Ask the server to clone a specific object
		/// </summary>
		/// <param name="toClone">GameObject to clone, must have a network identity</param>
		/// <param name="worldPosition">world position to spawn it at</param>
		/// <returns></returns>
		public static void Send(GameObject toClone, Vector2 worldPosition)
		{

			NetMessage msg = new NetMessage
			{
				ToClone = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				WorldPosition = worldPosition,
			};

			Send(msg);
		}
	}
}
