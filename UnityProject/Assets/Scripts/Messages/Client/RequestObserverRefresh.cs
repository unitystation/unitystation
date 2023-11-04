using Logs;
using Mirror;
using UnityEngine.SceneManagement;

namespace Messages.Client
{
	/// <summary>
	/// It is up to the client to let us know when they have
	/// finished loading/transitioning to a new subscene
	/// </summary>
	public class RequestObserverRefresh : ClientMessage<RequestObserverRefresh.NetMessage>
	{
		public struct NetMessage: NetworkMessage
		{
			/// <summary>
			/// The new scene we are requesting to observe
			/// </summary>
			public string NewSceneNameContext;
		}

		//TODO OldSceneNameContext (the scene we want to stop observing)
		public override void Process(NetMessage msg)
		{
			var sceneContext = SceneManager.GetSceneByName(msg.NewSceneNameContext);

			if (!sceneContext.IsValid())
			{
				Loggy.LogError( msg.NewSceneNameContext + " < No scene was found for Observer refresh!!", Category.Connections);
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
		public static NetMessage Send(string newSceneNameContext)
		{
			NetMessage msg = new NetMessage
			{
				NewSceneNameContext = newSceneNameContext
			};

			Send(msg);
			return msg;
		}
	}
}
