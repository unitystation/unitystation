using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

public class Managers : MonoBehaviour
{
	public static Managers instance;

	public string serverIP;
	[Header("For turning UI on and off to free up the editor window")] public GameObject UIParent;
	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			//Spawn the Options Screen Object
			Instantiate(Resources.Load("OptionsScreen"));
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Start()
	{
		Application.runInBackground = true;
		DontDestroyOnLoad(gameObject);
	}

	public async void SetScreenForGame()
	{
		//Called by GameData
		UIParent.SetActive(true);
		UIManager.Display.SetScreenForGame();

		await Task.Delay(3000); //Wait a decent amount of time for startup of the scene (3s)

		Instantiate (Resources.Load ("UI/GUI/Right click canvas"));
		if (CustomNetworkManager.Instance._isServer)
		{
			//Spawn the ProgressBar handler:
			var p = PoolManager.PoolNetworkInstantiate(Resources.Load("ProgressBar") as GameObject, Vector3.zero);
		}
	}

	public void SetScreenForLobby()
	{
		//Called by GameData
		UIParent.SetActive(true);
		UIManager.Display.SetScreenForLobby();
	}

	private void OnApplicationQuit()
	{
		if (!Application.isEditor)
		{
			Process.GetCurrentProcess().Kill();
		}
	}
}