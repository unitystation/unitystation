using Items;
using UnityEditor;
using UnityEngine;

public class Networking : Editor
{
    [MenuItem("Networking/Pickup Random Item (Client)")]
    private static void PickupRandomItem()
    {
        var items = FindObjectsOfType<PickUpTrigger>();
        var gameObject = items[Random.Range(1, items.Length)].gameObject;
        InteractMessage.Send(gameObject, "id");
    }

    [MenuItem("Networking/Give Random Item To All (Server)")]
    private static void GiveItems()
    {
        var players = FindObjectsOfType<PlayerNetworkActions>();
        var items = FindObjectsOfType<PickUpTrigger>();

        //		var gameObject = items[Random.Range(1, items.Length)].gameObject;
        for (var i = 0; i < players.Length; i++)
        {
            var gameObject = items[Random.Range(1, items.Length)].gameObject;
            players[i].AddItem(gameObject, "leftHand", true);
        }
    }

    [MenuItem("Networking/Gib All (Server)")]
    private static void GibAll()
    {
        GibMessage.Send();
    }
}