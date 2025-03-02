using Mirror;

public class SubSceneManagerNetworked : NetworkBehaviour
{
	[SyncVar] public bool ScenesInitialLoadingComplete = false;


	public readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	public SubSceneManager SubSceneManager;

	//Note: This is the first thing ever that gets called on the server after managers finish setting up.
	//This is where life blooms.
	public override void OnStartServer()
	{
		NetworkServer.observerSceneList.Clear();
		// Determine a Main station subscene and away site
		Destroy(Managers.GameManager.Instance.GameMode);
		Managers.GameManager.Instance.GameMode = null;
		Managers.GameManager.Instance.ChooseGameMode();
		StartCoroutine(SubSceneManager.RoundStartServerLoadSequence());
		base.OnStartServer();
	}

}
