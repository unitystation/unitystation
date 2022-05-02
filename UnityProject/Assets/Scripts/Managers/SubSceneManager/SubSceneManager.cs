using System;
using System.Collections;
using Messages.Client;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SubSceneManager : NetworkBehaviour
{
	private static SubSceneManager subSceneManager;

	public static SubSceneManager Instance;

	public AwayWorldListSO awayWorldList;
	[SerializeField] private MainStationListSO mainStationList = null;
	[SerializeField] private AsteroidListSO asteroidList = null;
	[SerializeField] private AdditionalSceneListSO additionalSceneList = null;

	public readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	public MainStationListSO MainStationList => mainStationList;

	public bool AwaySiteLoaded { get; private set; }
	public bool MainStationLoaded { get; private set; }

	public bool SyndicateLoaded { get; private set; }
	public Scene SyndicateScene { get; private set; }
	public bool WizardLoaded { get; private set; }

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		EventManager.AddHandler(Event.RoundEnded, KillClientCoroutine);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		EventManager.RemoveHandler(Event.RoundEnded, KillClientCoroutine);
	}

	void KillClientCoroutine() //So the client isn't loading scenes while server is Loading a new round
	{
		ClientSideFinishAction?.Invoke();
		KillClientLoadingCoroutine = true;
	}

	void UpdateMe()
	{
		MonitorServerSceneListOnClient();
	}

	/// <summary>
	/// General subscene loader
	/// </summary>
	/// <param name="sceneName"></param>
	/// <returns></returns>
	IEnumerator LoadSubScene(string sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true)
	{
		AsyncOperation AO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		if (AO == null) yield break; // Null if scene not found.

		while (AO.isDone == false)
		{
			if (loadTimer != null) loadTimer.IncrementLoadBar();
			yield return WaitFor.EndOfFrame;
		}

		if (loadTimer != null) loadTimer.IncrementLoadBar();
		if (isServer)
		{
			NetworkServer.SpawnObjects();
		}
		else
		{
			if (HandlSynchronising)
			{
				NetworkClient.PrepareToSpawnSceneObjects();
				yield return WaitFor.Seconds(0.2f);
				RequestObserverRefresh.Send(sceneName);
			}
		}
	}

	public static void ProcessObserverRefreshReq(ConnectedPlayer connectedPlayer, Scene sceneContext)
	{
		if (connectedPlayer.Connection != null)
		{
			Instance.AddObserverToAllObjects(connectedPlayer.Connection, sceneContext);
		}
	}

}

public enum SceneType
{
	MainStation,
	AwaySite,
	Asteroid,
	AdditionalScenes,
	Space,
	HiddenScene
}

[System.Serializable]
public struct SceneInfo : IEquatable<SceneInfo>
{
	public string SceneName;
	public SceneType SceneType;

	public bool Equals(SceneInfo other)
	{
		return SceneName == other.SceneName && SceneType == other.SceneType;
	}

	public override bool Equals(object obj)
	{
		return obj is SceneInfo other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return ((SceneName != null ? SceneName.GetHashCode() : 0) * 397) ^ (int) SceneType;
		}
	}
}

[System.Serializable]
public class ScenesSyncList : SyncList<SceneInfo>
{
}

public class SubsceneLoadTimer
{
	public float MaxLoadTime;
	public float CurrentLoadTime;

	private string lastText;

	public void IncrementLoadBar(string text = "")
	{
		var textToDisplay = "";
		if (string.IsNullOrEmpty(text))
		{
			textToDisplay = lastText;
		}
		else
		{
			textToDisplay = text;
		}

		lastText = textToDisplay;

		CurrentLoadTime += 1f;
		UIManager.Display.preRoundWindow.UpdateLoadingBar(textToDisplay, CurrentLoadTime / MaxLoadTime);
	}
}