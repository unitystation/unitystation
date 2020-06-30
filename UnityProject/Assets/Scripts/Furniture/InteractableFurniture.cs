using System;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class InteractableFurniture : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[Tooltip("What it's made of.")]
	public GameObject resourcesMadeOf;

	[Tooltip("How many will it drop on deconstruct.")]
	public int howMany = 1;

	private Integrity integrity;

	private void Start()
	{
		integrity = gameObject.GetComponent<Integrity>();
		integrity.OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab(resourcesMadeOf, gameObject.TileWorldPosition().To3Int(), transform.parent,
			count: Random.Range(0, howMany+1), scatterRadius: Random.Range(0f,2f));
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
			.Any(pm => pm.IsBuckled))
		{
			return false;
		}
		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench, screwdriver in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) { return false; }
		return true;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f, sourceObj: gameObject);
			Disassemble(interaction);
		}

	}
	[Server]
	private void Disassemble(HandApply interaction)
	{
		Spawn.ServerPrefab(resourcesMadeOf, gameObject.WorldPosServer() , count: howMany);
		Despawn.ServerSingle(gameObject);
	}

}
