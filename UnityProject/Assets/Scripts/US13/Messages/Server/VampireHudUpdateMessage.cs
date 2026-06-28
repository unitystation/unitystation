
using Mirror;
using US13.Player;
using US13.Systems.Antagonists;

namespace US13.Messages.Server
{
	public class VampireHudUpdateMessage : ServerMessage<VampireHudUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int stage;
			public float minNeededForStage;
			public float currentCorruption;
			public float neededCorruption;
		}

		public override void Process(NetMessage msg)
		{
			PlayerScript localPlayer = PlayerManager.LocalPlayerScript;
			if (localPlayer.TryGetComponent<VampireStageProgression>(out var vampirePlayer) == false) return;
			vampirePlayer.UpdateHudProgress(msg.stage, msg.currentCorruption, msg.minNeededForStage, msg.neededCorruption);
		}

		public static void SendTo(NetworkConnectionToClient conn, int _stage, float currentAmount, float minAmount, float maxAmount)
		{
			var msg = new NetMessage
			{
				stage = _stage,
				currentCorruption = currentAmount,
				minNeededForStage = minAmount,
				neededCorruption =  maxAmount
			};
			SendTo(conn, msg);
		}
	}
}