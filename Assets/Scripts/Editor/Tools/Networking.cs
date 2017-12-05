using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Networking : Editor
{
    [MenuItem("Networking/Pickup Random Item (Client)")]
    static void PickupRandomItem()
    {
        var items = FindObjectsOfType<Items.PickUpTrigger>();
        var gameObject = items[Random.Range(1, items.Length)].gameObject;
        InteractMessage.Send(gameObject, "id");
    }
    [MenuItem("Networking/Give Random Item To All (Server)")]
    static void GiveItems()
    {
        var players = FindObjectsOfType<PlayerNetworkActions>();
        var items = FindObjectsOfType<Items.PickUpTrigger>();

        //		var gameObject = items[Random.Range(1, items.Length)].gameObject;
        for (var i = 0; i < players.Length; i++)
        {
            var gameObject = items[Random.Range(1, items.Length)].gameObject;
            players[i].AddItem(gameObject, "leftHand", true);
        }
    }

    [MenuItem("Networking/Gib All (Server)")]
    static void GibAll()
    {
        GibMessage.Send();
    }
}
