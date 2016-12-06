using UnityEngine;
using System.Collections;
using Network;

public class SceneLoader: MonoBehaviour {

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void GoToKitchen() {

        SoundManager.control.sounds["Click01"].Play();
        NetworkManager.control.LoadMap();
        Debug.Log("GO TO THE GAME");



    }

    public void GoToLobby() {
        SoundManager.control.sounds["Click01"].Play();

        NetworkManager.control.LeaveMap(); // Leave the game on the server and also on the client
    }
}
