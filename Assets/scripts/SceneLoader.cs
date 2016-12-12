using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using PlayGroup;
using Network;

public class SceneLoader: MonoBehaviour {

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void GoToKitchen() {
        SoundManager.control.Play("Click01");
        NetworkManager.control.LoadMap();
        Debug.Log("GO TO THE GAME");



    }

    public void GoToLobby() {
        SoundManager.control.Play("Click01");
        SceneManager.LoadSceneAsync ("Lobby");
        PlayerManager.control.hasSpawned = false;
        //        NetworkManager.control.LeaveMap(); // Leave the game on the server and also on the client (this is for quitting only! it shouldn't be here)
    }
}
