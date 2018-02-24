using Items;
using UnityEditor;
using UnityEngine;

public class Networking : Editor
{
	[MenuItem("Networking/Pickup Random Item (Client)")]
	private static void PickupRandomItem()
	{
		PickUpTrigger[] items = FindObjectsOfType<PickUpTrigger>();
		GameObject gameObject = items[Random.Range(1, items.Length)].gameObject;
		InteractMessage.Send(gameObject, "id");
	}

	[MenuItem("Networking/Give Random Item To All (Server)")]
	private static void GiveItems()
	{
		PlayerNetworkActions[] players = FindObjectsOfType<PlayerNetworkActions>();
		PickUpTrigger[] items = FindObjectsOfType<PickUpTrigger>();

		//		var gameObject = items[Random.Range(1, items.Length)].gameObject;
		for (int i = 0; i < players.Length; i++)
		{
			GameObject gameObject = items[Random.Range(1, items.Length)].gameObject;
			players[i].AddItem(gameObject, "leftHand", true);
		}
	}

	[MenuItem("Networking/Transform Waltz (Server)")]
	private static void MoveAll()
	{
		CustomNetworkManager.Instance.MoveAll();
	}

	[MenuItem("Networking/Gib All (Server)")]
	private static void GibAll()
	{
		GibMessage.Send();
	}
}