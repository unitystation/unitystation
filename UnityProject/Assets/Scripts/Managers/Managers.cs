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

	public void SetScreenForGame()
	{
		//Called by GameData
		UIParent.SetActive(true);
		UIManager.Display.SetScreenForGame();
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