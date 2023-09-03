using Logs;
using UnityEngine;
using Mirror;


namespace Messages.Client.DevSpawner
{
	/// <summary>
	/// Message allowing a client dev / admin to clone something, validated server side.
	/// </summary>
	public class DevDestroyMessage : ClientMessage<DevDestroyMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// Net ID of the object to destroy
			public uint ToDestroy;

			public override string ToString()
			{
				return $"[DevDestroyMessage ToClone={ToDestroy}]";
			}
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			if (msg.ToDestroy.Equals(NetId.Invalid))
			{
				Loggy.LogWarning("Attempted to destroy an object with invalid netID, destroy will not occur.", Category.Admin);
			}
			else
			{
				LoadNetworkObject(msg.ToDestroy);

				if (NetworkObject == null) return;

				Vector2Int worldPos = NetworkObject.transform.position.RoundTo2Int();
				if (NetworkObject.TryGetComponent<PlayerScript>(out var victim))
				{
					victim.playerHealth.OnGib();
					UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
						$"{SentByPlayer.Username} gibbed {victim.playerName} at {worldPos} using the dev destroyer tool.", SentByPlayer.UserId);
					return;
				}
				UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
					$"{SentByPlayer.Username} destroyed a {NetworkObject} at {worldPos}", SentByPlayer.UserId);
				_ = Despawn.ServerSingle(NetworkObject);
			}
		}

		/// <summary>
		/// Ask the server to destroy a specific object
		/// </summary>
		/// <param name="toClone">GameObject to destroy, must have a network identity</param>
		/// <returns></returns>
		public static void Send(GameObject toClone)
		{

			NetMessage msg = new NetMessage
			{
				ToDestroy = toClone ? toClone.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			};
			Send(msg);
		}
	}
}
