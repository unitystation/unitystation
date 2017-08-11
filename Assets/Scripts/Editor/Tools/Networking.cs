using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Networking : Editor {
	[MenuItem("Networking/Pickup Random Item (Client)")]
	static void PickupRandomItem() {
		var items = Object.FindObjectsOfType<Items.PickUpTrigger>();
		var gameObject = items[Random.Range(1, items.Length)].gameObject;
		InteractMessage.Send(gameObject, "id");
	}

	[MenuItem("Networking/Gib All (Server)")]
	static void GibAll() {
		GibMessage.Send();
	}
}
