using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour {

    public GameObject playerCamera;
    public float panSpeed = 10.0f;
    private SpriteRenderer playerRend;
    private SpriteRenderer suitRend;
    private SpriteRenderer beltRend;
    private SpriteRenderer feetRend;
    private SpriteRenderer headRend;
    private SpriteRenderer faceRend;
    private SpriteRenderer maskRend;
    private SpriteRenderer underwearRend;
    private SpriteRenderer uniformRend;

    private Sprite[] playerSheet;
    private Sprite[] suitSheet;
    private Sprite[] beltSheet;
    private Sprite[] feetSheet;
    private Sprite[] headSheet;
    private Sprite[] faceSheet;
    private Sprite[] maskSheet;
    private Sprite[] underwearSheet;
    private Sprite[] uniformSheet;


    // Use this for initialization
    void Start () {
        playerRend = GetComponent<SpriteRenderer>();
       
        foreach(SpriteRenderer child in this.GetComponentsInChildren<SpriteRenderer>())
        {
            Debug.Log(child.name + " " + child.tag);
            switch (child.name)
            {
                case "suit":
                    suitRend = child;
                    break;
                case "belt":
                    beltRend = child;
                    break;
                case "feet":
                    feetRend = child;
                    break;
                case "head":
                    headRend = child;
                    break;
                case "face":
                    faceRend = child;
                    break;
                case "mask":
                    maskRend = child;
                    break;
                case "underwear":
                    underwearRend = child;
                    break;
                case "uniform":
                    uniformRend = child;
                    break;
            }
            
        }
        playerSheet = Resources.LoadAll<Sprite>("mobs/human");
        suitSheet = Resources.LoadAll<Sprite>("mobs/suit");
        beltSheet = Resources.LoadAll<Sprite>("mobs/belt");
        feetSheet = Resources.LoadAll<Sprite>("mobs/feet");
        headSheet = Resources.LoadAll<Sprite>("mobs/head");
        faceSheet = Resources.LoadAll<Sprite>("mobs/human_face");
        maskSheet = Resources.LoadAll<Sprite>("mobs/mask");
        underwearSheet = Resources.LoadAll<Sprite>("mobs/underwear");
        uniformSheet = Resources.LoadAll<Sprite>("mobs/uniform");
        //populate child sprites chef suit onto player

    }

    // Update is called once per frame
    void Update () {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, moveVertical) * panSpeed;
        Vector2 moveDirection = new Vector2(movement.x, movement.y);
        Vector2 normalized = moveDirection.normalized;
        //Debug.Log(normalized.x +", "+ normalized.y);
        if (normalized == Vector2.down)
        {
            playerRend.sprite = playerSheet[36];
            suitRend.sprite = suitSheet[236];
            beltRend.sprite = beltSheet[62];
            headRend.sprite = headSheet[221];
            feetRend.sprite = feetSheet[36];
            underwearRend.sprite = underwearSheet[52];
            uniformRend.sprite = uniformSheet[16];
        }
        if (normalized == Vector2.up)
        {
            playerRend.sprite = playerSheet[37];
            suitRend.sprite = suitSheet[237];
            beltRend.sprite = beltSheet[63];
            headRend.sprite = headSheet[222];
            feetRend.sprite = feetSheet[37];
            underwearRend.sprite = underwearSheet[53];
            uniformRend.sprite = uniformSheet[17];
        }
        if (normalized == Vector2.right)
        {
            playerRend.sprite = playerSheet[38];
            suitRend.sprite = suitSheet[238];
            beltRend.sprite = beltSheet[64];
            headRend.sprite = headSheet[223];
            feetRend.sprite = feetSheet[38];
            underwearRend.sprite = underwearSheet[54];
            uniformRend.sprite = uniformSheet[18];
        }
        if (normalized == Vector2.left)
        {
            playerRend.sprite = playerSheet[39];
            suitRend.sprite = suitSheet[239];
            beltRend.sprite = beltSheet[65];
            headRend.sprite = headSheet[224];
            feetRend.sprite = feetSheet[39];
            underwearRend.sprite = underwearSheet[55];
            uniformRend.sprite = uniformSheet[19];
        }




        transform.position = transform.position + movement;
        playerCamera.transform.position = playerCamera.transform.position + movement;

    }
}
