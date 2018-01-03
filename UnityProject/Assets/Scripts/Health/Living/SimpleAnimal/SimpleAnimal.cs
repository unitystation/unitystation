using System.Collections;
using Sprites;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleAnimal : HealthBehaviour
{
	public Sprite aliveSprite;
	[Header("For harvestable animals")] public GameObject[] butcherResults;
	public Sprite deadSprite;

	//Syncvar hook so that new players can sync state on start
	[SyncVar(hook = "SetAliveState")] public bool deadState;

	public SpriteRenderer spriteRend;

	private void Start()
	{
		//Set it automatically because we are using the SimpleAnimalBehaviour
		isNPC = true;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		StartCoroutine(WaitForLoad());
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		SetAliveState(deadState);
	}

	public override int ReceiveAndCalculateDamage(GameObject damagedBy, int damage, DamageType damageType,
		BodyPartType bodyPartAim)
	{
		base.ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);
		if (isServer)
		{
			EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
		}
		return damage;
	}

	[Server]
	public virtual void Harvest()
	{
		foreach (GameObject harvestPrefab in butcherResults)
		{
			ItemFactory.SpawnItem(harvestPrefab, transform.position, transform.parent);
		}
		EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
		//Remove the NPC after all has been harvested
		NetworkServer.Destroy(gameObject);
	}

	[Server]
	protected override void OnDeathActions()
	{
		deadState = true;
	}

	private void SetAliveState(bool state)
	{
		deadState = state;
		if (state)
		{
			spriteRend.sprite = deadSprite;
		}
		else
		{
			spriteRend.sprite = aliveSprite;
		}
	}
}