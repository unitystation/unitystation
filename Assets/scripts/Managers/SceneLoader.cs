using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using PlayGroup;
using Network;

public class SceneLoader: MonoBehaviour {    
    void Start() {

    }
    
    void Update() {

    }

    public void GoToKitchen() {
        SoundManager.Play("Click01");
        NetworkManager.LoadMap();
        Debug.Log("GO TO THE GAME");
    }

    public void GoToLobby() {
        SoundManager.Play("Click01");
        SceneManager.LoadSceneAsync("Lobby");
        PlayerManager.Reset();
        //        NetworkManager.control.LeaveMap(); // Leave the game on the server and also on the client (this is for quitting only! it shouldn't be here)
    }
}
