using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MedibotConstruct : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public GameObject mediConstruct;
	//remove after problem is fixed
	private bool interactCheck = false;


	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		Debug.Log("This runs!");

		// Checks to make sure player is next to object is concious 
		if (!DefaultWillInteract.Default(interaction, side)) interactCheck = false;

		var sheet = interaction.HandObject != null ? interaction.HandObject.GetComponent<ItemAttributesV2>() : null;
		var slots = gameObject.GetComponent<ItemStorage>()?.GetItemSlots();
		foreach (var _ in
		// This shit still broken, cant detect empty container
		from ItemSlot slot in slots
		where slot.Item != null
		select new { })
		{
			interactCheck = false;
			return interactCheck;
		}

		if (sheet != null & slots == null)
		{
			if (sheet.InitialName == "metal sheet") interactCheck = true;
			Debug.LogFormat("Stage 1 reads: {0}", interactCheck);
			return interactCheck;
		}
		else
		{
			Debug.LogFormat("Stage 1 reads: {0}", interactCheck);
			interactCheck = false;
			return interactCheck;
		}

	}


	public void ClientPredictInteraction(HandApply interaction)
	{
		Spawn.ServerPrefab(mediConstruct, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
		Despawn.ServerSingle(gameObject);
	}
	public void ServerRollbackClient(HandApply interaction)
	{
		Debug.LogError("Warning, rollback detected!");
		//Ill likely need this later to fix desyncs
		Debug.LogFormat("Stage 3 reads: {0}", interactCheck);
	}

	//invoked when the server recieves the interaction request and WIllinteract returns true
	public void ServerPerformInteraction(HandApply interaction)
	{
		Debug.Log("Server Preform interaction");
		Debug.LogFormat("Stage 4 reads: {0}", interactCheck);
		Spawn.ServerPrefab(mediConstruct, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
		Despawn.ServerSingle(gameObject);
	}
}