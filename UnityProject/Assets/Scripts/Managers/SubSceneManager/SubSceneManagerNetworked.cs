using Mirror;

public class SubSceneManagerNetworked : NetworkBehaviour
{
	[SyncVar] public bool ScenesInitialLoadingComplete = false;


	public readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	public SubSceneManager SubSceneManager;

	public override void OnStartServer()
	{
		NetworkServer.observerSceneList.Clear();
		// Determine a Main station subscene and away site
		GameManager.Instance.ChooseGameMode();
		StartCoroutine(SubSceneManager.RoundStartServerLoadSequence());
		base.OnStartServer();
	}

}
