using Messages.Client;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A client asks a server to give the client available(possible) reagents, so the client may update
	/// 	its recipe button borders. This ClientMessage is designed to handle it.
	/// </summary>
	public class RequestRefreshRecipes : ClientMessage<RequestRefreshRecipes.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			if (
				Cooldowns.TryStartServer(
					SentByPlayer.Script,
					CommonCooldowns.Instance.Interaction
				) == false
			)
			{
				return;
			}
			SendRefreshRecipesOrder.SendTo(SentByPlayer);
		}

		public static void Send()
		{
			if (
				Cooldowns.TryStartClient(
					PlayerManager.LocalPlayerScript,
					CommonCooldowns.Instance.Interaction
				) == false
			)
			{
				return;
			}

			Send(new NetMessage());
		}
	}
}