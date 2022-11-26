using Messages.Client;
using Mirror;
using UI.Core.Action;

public class RequestIconsUIActionRefresh : ClientMessage<RequestIconsUIActionRefresh.NetMessage>
{
	public struct NetMessage : NetworkMessage { }

	public override void Process(NetMessage msg)
	{
		if (SentByPlayer.Mind == null) return;

		var bodies = SentByPlayer.Mind.GetRelatedBodies();
		foreach (var body in bodies)
		{
			UIActionManager.Instance.UpdatePlayer(body.gameObject, SentByPlayer.Connection);
		}


	}

	public static NetMessage Send()
	{
		NetMessage msg = new NetMessage();
		Send(msg);
		return msg;
	}
}
