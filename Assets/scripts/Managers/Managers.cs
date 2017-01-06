using UnityEngine;
using System.Collections;
using PlayGroup;
using UI;

public class Managers: MonoBehaviour {
    // Use this for initialization

    [Header("For turning UI on and off to free up the editor window")]
    public GameObject UIParent;

    public bool isDevMode = false;

    private static Managers managers;

    public static Managers Instance {
        get {
            if(!managers) {
                managers = FindObjectOfType<Managers>();

                managers.Init();
            }
            return managers;
        }
    }

    private void Init() {
        Application.runInBackground = true;
        DontDestroyOnLoad(gameObject);
    }

    public static bool IsDevMode {
        get {
            return Instance.isDevMode;
        }
    }

    public static void SetScreenForGame() { //Called by GameData
        Instance.UIParent.SetActive(true);
        UIManager.Display.SetScreenForGame();
        PlayerManager.CheckIfSpawned(); // See if we have already spawned a player, if not then spawn one
    }

    public static void SetScreenForLobby() { //Called by GameData
        Instance.UIParent.SetActive(true);
        UIManager.Display.SetScreenForLobby();
    }
}
