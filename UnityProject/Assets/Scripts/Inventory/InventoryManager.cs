using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
	private static InventoryManager inventoryManager;

	public static InventoryManager Instance
	{
		get
		{
			if (!inventoryManager)
			{
				inventoryManager = FindObjectOfType<InventoryManager>();
			}
			return inventoryManager;
		}
	}

	//Clientside only:
	public static List<InventorySlot> AllClientInventorySlots = new List<InventorySlot>();
	//Server holds all slots for all the clients:
	public static List<InventorySlot> AllServerInventorySlots = new List<InventorySlot>();

	void OnEnable()
	{
		SceneManager.activeSceneChanged += Instance.OnSceneChange;
	}

	void OnDisable()
	{
		SceneManager.activeSceneChanged -= Instance.OnSceneChange;
	}

	public void OnSceneChange(Scene lastScene, Scene newScene)
	{
		AllClientInventorySlots.Clear();
		AllServerInventorySlots.Clear();
	}
}

//Helps identify the position in syncEquip list
public enum Epos
{
	suit,
	belt,
	head,
	feet,
	face,
	mask,
	uniform,
	leftHand,
	rightHand,
	eyes,
	back,
	hands,
	ear,
	neck
}