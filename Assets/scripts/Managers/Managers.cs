using UnityEngine;
using System.Collections;
using PlayGroup;
using UI;

public class Managers: MonoBehaviour
{
    // Use this for initialization

    [Header("For turning UI on and off to free up the editor window")]
    public GameObject UIParent;

    public bool isDevMode = false;

    public static Managers instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

    }

    void Start()
    {
        Application.runInBackground = true;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsDevMode
    {
        get
        {
            return isDevMode;
        }
    }

    public void SetScreenForGame()
    { //Called by GameData
       UIParent.SetActive(true);
        UIManager.Display.SetScreenForGame();
        PlayerManager.CheckIfSpawned(); // See if we have already spawned a player, if not then spawn one
    }

    public void SetScreenForLobby()
    { //Called by GameData
        UIParent.SetActive(true);
        UIManager.Display.SetScreenForLobby();
    }
}
