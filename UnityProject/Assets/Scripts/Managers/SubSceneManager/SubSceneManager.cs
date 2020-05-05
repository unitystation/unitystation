using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSceneManager : MonoBehaviour
{
	private static SubSceneManager subSceneManager;
	public static SubSceneManager Instance
	{
		get
		{
			if (subSceneManager == null)
			{
				subSceneManager = FindObjectOfType<SubSceneManager>();
			}

			return subSceneManager;
		}
	}

	[SerializeField] private AwayWorldListSO awayWorldList;

	private string serverChosenAwaySite;
	public string ServerChosenAwaySite
	{
		get => serverChosenAwaySite;
	}
	public bool ServerAwaySiteLoaded { get; private set; }

	[Tooltip("Plan the best time to start loading the away world on the " +
	         "server/host. This should be done slighty after main scene load and before " +
	         "round start.")]
	[SerializeField] private float serverWaitAwayWorldLoad = 10f;

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
	}

	void OnSceneChange(Scene oldScene, Scene newScene)
	{
		serverChosenAwaySite = "";
		ServerAwaySiteLoaded = false;
		if (newScene.name != "Lobby")
		{
			StartCoroutine(WaitForMatrixManager());
		}
	}

	IEnumerator WaitForMatrixManager()
	{
		while (!MatrixManager.IsInitialized)
		{
			yield return WaitFor.EndOfFrame;
		}

		if (CustomNetworkManager.Instance._isServer)
		{
			serverChosenAwaySite = awayWorldList.GetRandomAwaySite();
			StartCoroutine(RoundStartServerLoadSequence());
		}
	}

	IEnumerator RoundStartServerLoadSequence()
	{
		yield return WaitFor.Seconds(serverWaitAwayWorldLoad);
		//Load the away site
		yield return StartCoroutine(LoadSubScene(serverChosenAwaySite));
		ServerAwaySiteLoaded = true;
		Logger.Log($"Server has loaded {serverChosenAwaySite} away site", Category.SubScenes);
	}

	IEnumerator LoadSubScene(string sceneName)
	{
		AsyncOperation AO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		while(!AO.isDone)
		{
			yield return WaitFor.EndOfFrame;
		}
		yield return WaitFor.EndOfFrame;

		NetworkServer.SpawnObjects();
		yield return WaitFor.EndOfFrame;
		// SceneMessage msg = new SceneMessage
		// {
		// 	sceneName = worldScene,
		// 	sceneOperation = SceneOperation.LoadAdditive
		// };
		//
		// NetworkServer.SendToAll(msg);
	}
}
