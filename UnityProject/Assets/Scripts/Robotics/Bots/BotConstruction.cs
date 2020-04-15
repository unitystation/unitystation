using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class BotConstruction : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[Tooltip("Place the parts used in each stage, the first part will be element 0")]
	public GameObject[] stageParts;//Add gameprefabs here

	[Tooltip("Place each sprite for each stage here, if the sprite should stay the same just put the same sprite in")]
	public Sprite[] stageSprite; //Sprites used for each stage

	public GameObject botPrefab; //The bot spawned after all stages are done

	[SyncVar]
	private int stageCounter = 0; //The counter that will be used to figure out what stage the bot is on

	public SpriteHandler spriteHandler;



	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		Debug.Log("This runs!");
		Debug.LogFormat("Stage at: {0}", stageCounter);
		// Checks to make sure player is next to object is concious 
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//Grabs the attributes of the item in active hand
		var item = interaction.HandObject != null ? interaction.HandObject.GetComponent<ItemAttributesV2>().InitialName : null;
		// Goes through list of items and checks them against the stageParts list and stageCounter
		for (int x = 0; x < stageParts.Length - 1; x++)
		{
			Debug.LogWarningFormat("Loop is on {0}",x);
			var checkItem = stageParts[stageCounter].GetComponent<ItemAttributesV2>().InitialName;
			if (item == checkItem & x == stageCounter) return true;
			if (x == stageParts.Length - 1)
			{
				Debug.LogErrorFormat("Returned False! Item wanted is:{0} Item got is{1} Stage is {2} List is on {3}", checkItem, item, stageCounter, x);
				return false;
			}
		}
		Debug.LogError("BotConstruction should never get to this point, if you see this get a developer to look at BotConstruction.cs");
		return false;
	}


	public void ClientPredictInteraction(HandApply interaction)
	{
		Debug.LogWarning("Client predicting!");
	}
	public void ServerRollbackClient(HandApply interaction)
	{
		Debug.LogError("Rollback found!");
	}

	//invoked when the server recieves the interaction request and WIllinteract returns true
	public void ServerPerformInteraction(HandApply interaction)
	{
		Debug.LogWarningFormat("Serverside is running! Stage at {0}", stageCounter);
		Debug.LogWarningFormat("Stageparts is: {0}", stageParts.Length - 1);
		if (stageCounter >= stageParts.Length - 1)
		{
			Spawn.ServerPrefab(botPrefab, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
			Despawn.ServerSingle(gameObject);
		}
		else stageCounter++;
		Debug.LogWarningFormat("Stage Check: {0}", stageCounter);
	}
}
