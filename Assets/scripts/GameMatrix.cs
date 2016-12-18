using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMatrix : MonoBehaviour
{

    public static GameMatrix control;

    public Dictionary<int,GameObject> items = new Dictionary<int,GameObject>(); //items
    public Dictionary<int,Cupboards.DoorTrigger> cupboards = new Dictionary<int,Cupboards.DoorTrigger>(); //cupboards

    void Awake()
    {

        if (control == null)
        {
            control = this;
        }
        else
        {
            Destroy(this);
        }
    }

    //Add each item to the items dictionary along with their photonView.viewID as key
    public void AddItem(int viewID, GameObject theItem)
    {
        items.Add(viewID, theItem);
    }

    //Add each cupB to the items dictionary along with its photonView.viewID as key (this will be doortriggers)
    public void AddCupboard(int viewID, Cupboards.DoorTrigger theCupB) //To get transform.position then look at the DoorTrigger.transform.parent
    {
        cupboards.Add(viewID, theCupB);
    }

}
