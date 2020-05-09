using Mirror;
using UnityEngine.SceneManagement;

/// <summary>
/// It is up to the client to let us know when they have
/// finished loading/transitioning to a new subscene
/// </summary>
public class RequestObserverRefresh : ClientMessage
{
	/// <summary>
	/// The new scene we are requesting to observe
	/// </summary>
	public string NewSceneNameContext;

	//TODO OldSceneNameContext (the scene we want to stop observing)

	public override void Process()
	{
		var sceneContext = SceneManager.GetSceneByName(NewSceneNameContext);

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
	public static RequestObserverRefresh Send(string newSceneNameContext)
	{
		RequestObserverRefresh msg = new RequestObserverRefresh
		{
			NewSceneNameContext = newSceneNameContext
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		NewSceneNameContext = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(NewSceneNameContext);
	}
}
