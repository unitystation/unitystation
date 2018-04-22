using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	public interface IPlayerSync
	{
		GameObject PullingObject { get; set; }
		NetworkInstanceId PullObjectID { get; set; }
		void CmdSetPositionFromReset(GameObject fromObj, GameObject otherPlayer, Vector3 setPos);

		/// <summary>
		///     Manually set a player to a specific position
		/// </summary>
		/// <param name="worldPos">The new position to "teleport" player</param>
		void SetPosition(Vector3 worldPos);

		void PullReset(NetworkInstanceId netID);
//		bool InvokeCommand(int cmdHash, NetworkReader reader);
//		bool InvokeRPC(int cmdHash, NetworkReader reader);
//		bool InvokeSyncEvent(int cmdHash, NetworkReader reader);
//		bool InvokeSyncList(int cmdHash, NetworkReader reader);
		void ProcessAction(PlayerAction action);
		void UpdateClientState(PlayerState state);
		void ClearQueueClient();
		void Push(Vector2Int direction);
		///For server code. Contains position
		PlayerState ServerState { get; }
		/// For client code
		PlayerState ClientState { get; }
	}
}