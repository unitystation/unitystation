using Mirror;

/// <summary>
/// It is up to the client to let us know when they have
/// finished loading/transitioning to a new subscene
/// </summary>
public class RequestObserverRefresh : ClientMessage
{
	public ObserverRequest RequestType;
	public override void Process()
	{
		SubSceneManager.ProcessObserverRefreshReq(SentByPlayer, RequestType);
	}

	public static RequestObserverRefresh Send(ObserverRequest requestType)
	{
		RequestObserverRefresh msg = new RequestObserverRefresh
		{
			RequestType = requestType
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		RequestType = (ObserverRequest)reader.ReadInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteInt32((int)RequestType);
	}
}

/// <summary>
/// What is the nature of the refresh request
/// </summary>
public enum ObserverRequest
{
	None,
	OnlineSceneRefresh,
	RefreshForMainStation,
	RefreshForAwaySite
}
