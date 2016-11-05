using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour {

    public GameObject playerCamera;
    public float panSpeed = 10.0f;
    private SpriteRenderer thisRend;

    // Use this for initialization
    void Start () {
        thisRend = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update () {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, moveVertical) * panSpeed;

        
        transform.position = transform.position + movement;
        playerCamera.transform.position = playerCamera.transform.position + movement;

    }
}
