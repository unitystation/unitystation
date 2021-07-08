using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScriptableObjects;
using NaughtyAttributes;

public class SpawnItemsArtifactEffect : ArtifactEffect
{
	public List<GameObject> itemsToSpawn = new List<GameObject>();

	public int howManyDifferentItems = 1;
	public int howManyOfThoseItems = 1;

	[Tooltip("If this is true then the two above Ints are the max values")]
	public bool randomSpawnAmounts = false;

	[ShowIf(nameof(randomSpawnAmounts))]
	public int minHowManyDifferentItems = 1;
	[ShowIf(nameof(randomSpawnAmounts))]
	public int minHowManyOfThoseItems = 1;

	public float scatterRadius = 1f;

	public string spawnMessage;

	public override void DoEffectTouch(HandApply touchSource)
	{
		base.DoEffectTouch(touchSource);
		TrySpawnItem();
	}

	private void TrySpawnItem()
	{
		var amount = randomSpawnAmounts
			? Random.Range(minHowManyDifferentItems, howManyDifferentItems + 1)
			: howManyDifferentItems;

		var amountItem = randomSpawnAmounts
			? Random.Range(minHowManyOfThoseItems, howManyOfThoseItems + 1)
			: howManyOfThoseItems;

		for (int i = 0; i < amount; i++)
		{
			Spawn.ServerPrefab(itemsToSpawn.GetRandom(), gameObject.WorldPosServer(), transform.parent.transform
				, count: amountItem, scatterRadius: scatterRadius);
		}

		if(string.IsNullOrEmpty(spawnMessage)) return;

		Chat.AddLocalMsgToChat(spawnMessage, gameObject);
	}
}
