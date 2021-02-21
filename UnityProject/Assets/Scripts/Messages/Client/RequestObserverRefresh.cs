using Messages.Client;
using Mirror;
using UnityEngine.SceneManagement;

/// <summary>
/// It is up to the client to let us know when they have
/// finished loading/transitioning to a new subscene
/// </summary>
public class RequestObserverRefresh : ClientMessage
{
	public struct RequestObserverRefreshNetMessage: NetworkMessage
	{
		/// <summary>
		/// The new scene we are requesting to observe
		/// </summary>
		public string NewSceneNameContext;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestObserverRefreshNetMessage IgnoreMe;

	//TODO OldSceneNameContext (the scene we want to stop observing)

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestObserverRefreshNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		var sceneContext = SceneManager.GetSceneByName(newMsg.NewSceneNameContext);

		if (!sceneContext.IsValid())
		{
			Logger.LogError("No scene was found for Observer refresh!!");
			return;
		}

		SubSceneManager.ProcessObserverRefreshReq(SentByPlayer, sceneContext);
	}

	/// <summary>
	/// Request to become an observer to networked objects in a
	/// particular scene
	/// </summary>
	/// <param name="newSceneNameContext"> The scene we are requesting to be observers of</param>
	/// <returns></returns>
	public static RequestObserverRefreshNetMessage Send(string newSceneNameContext)
	{
		RequestObserverRefreshNetMessage msg = new RequestObserverRefreshNetMessage
		{
			NewSceneNameContext = newSceneNameContext
		};
		new RequestObserverRefresh().Send(msg);
		return msg;
	}
}
