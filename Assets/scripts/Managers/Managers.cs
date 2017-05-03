using UnityEngine;
using System.Collections;
using PlayGroup;
using UI;

public class Managers: MonoBehaviour
{
	[Header("For turning UI on and off to free up the editor window")]
	public GameObject UIParent;

	public static Managers instance;

	void Awake()
	{
		if (instance == null) {
			instance = this;
		} else {
			Destroy(this.gameObject);
		}
	}

	void Start()
	{
		Application.runInBackground = true;
		DontDestroyOnLoad(gameObject);
	}

	public void SetScreenForGame()
	{ //Called by GameData

		UIParent.SetActive(true);
		UIManager.Display.SetScreenForGame();
	}

	public void SetScreenForLobby()
	{ //Called by GameData
		UIParent.SetActive(true);
		UIManager.Display.SetScreenForLobby();
	}
}
