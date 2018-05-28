using System.Collections.Generic;
using Crafting;
using Items;
using PlayGroup;
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
	[MenuItem("Networking/Push everyone up")]
	private static void PushEveryoneUp()
	{
		foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
		{
			player.GameObject.GetComponent<PlayerScript>().playerSync.Push(Vector2Int.up);
		}
	}
	[MenuItem("Networking/Spawn some meat")]
	private static void SpawnMeat()
	{
		foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
			Vector3 playerPos = player.GameObject.GetComponent<PlayerScript>().playerSync.ServerState.WorldPosition;
			Vector3 spawnPos = playerPos + new Vector3( 0, 2, 0 );
			GameObject mealPrefab = CraftingManager.Meals.FindOutputMeal("Meat Steak");
			var slabs = new List<CustomNetTransform>();
			for ( int i = 0; i < 5; i++ ) {
				slabs.Add( ItemFactory.Instance.SpawnMeal(mealPrefab, spawnPos, null).GetComponent<CustomNetTransform>() );
			}
			for ( var i = 0; i < slabs.Count; i++ ) {
				Vector3 vector3 = i%2 == 0 ? new Vector3(i,-i,0) : new Vector3(-i,i,0);
				slabs[i].ForceDrop( spawnPos + vector3/10 );
			}
		}
	}
	[MenuItem("Networking/Print player positions")]
	private static void PrintPlayerPositions()
	{
		//For every player in the connected player list (this list is serverside-only)
		foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
			
			//Get PlayerScript component that holds references for the other important player-related scripts
			var playerScript = player.GameObject.GetComponent<PlayerScript>();
			
			//Digging into PlayerSync component, grabbing ServerState and taking out current position
			Vector3 position = playerScript.playerSync.ServerState.Position;
			
			//Printing this the pretty way, example:
			//Bob (CAPTAIN) is located at (77,0, 52,0, 0,0)
			Debug.Log( $"{player.Name} ({player.Job}) is located at {position}" );
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
	[MenuItem("Networking/Restart round")]
	private static void AdminPlzRestart()
	{
		GameManager.Instance.RestartRound();
	}
	[MenuItem("Networking/Extend round time")]
	private static void ExtendRoundTime()
	{
		GameManager.Instance.ResetRoundTime();
	}
}