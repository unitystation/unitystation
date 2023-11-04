using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using HealthV2;
using Items;
using Items.Food;
using Mobs.BrainAI;
using UnityEngine;

public class BrainSlime : BrainMobState
{
	public AddressableAudioSource eatFoodSound;
	public LayerMask hitMask;

	public Dictionary<GameObject, int> GoodPlayers = new Dictionary<GameObject, int>();

	protected List<Vector3Int> Directions = new List<Vector3Int>()
	{
		new Vector3Int(1, 0, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 1, 0),
		new Vector3Int(0, -1, 0),
	};

	private SlimeCore SlimeCore;

	private SlimeEat SlimeEat;


	private readonly System.Random random = new System.Random();

	private float RecentDamage = 0;


	public bool FriendlySlime = false;

	public void IncomingDamage(DamageType DamageType, GameObject GameObject, float DMG)
	{
		RecentDamage += DMG;
	}

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		livingHealth.OnTakeDamageType -= IncomingDamage;
		SlimeEat = null;
	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		livingHealth.OnTakeDamageType += IncomingDamage;



		foreach (var BodyPart in livingHealth.BodyPartList)
		{
			SlimeEat = BodyPart.GetComponent<SlimeEat>();
			if (BodyPart != null) return;
		}
	} //Warning only add body parts do not remove body parts in this


	private void Awake()
	{
		SlimeCore = this.GetComponent<SlimeCore>();
	}

	public override void OnEnterState()
	{
		// No Behavior Required
	}

	public override void OnExitState()
	{
		// No Behavior Required
	}

	public override void OnUpdateTick()
	{
		if (SlimeEat == null) return;

		if (SlimeCore.CanSlimesSplit())
		{
			if (SlimeEat.CurrentlyEating != null)
			{
				SlimeEat.StopEating();
			}
			SlimeCore.SlimesSplit();
			return;
		}

		if (RecentDamage > 0)
		{
			if (RecentDamage > 5)
			{

				SlimeEat.StopEating();

				//Runaround
				var move = Directions[random.Next(0, Directions.Count)];
				master.Body.UniversalObjectPhysics.OrNull()?.TryTilePush(move.To2Int(), null);

				if (master.Body.Rotatable != null)
				{
					master.Body.Rotatable.SetFaceDirectionLocalVector(move.To2Int());
				}
			}

			RecentDamage--;
			if (RecentDamage < 0)
			{
				RecentDamage = 0;
			}
			return;
		}

		if (SlimeEat.CurrentlyEating != null)
		{
			return;
		}

		var players = Physics2D.OverlapCircleAll(master.Body.transform.position, 20f, hitMask);
		if (players.Length == 0 || FriendlySlime)
		{
			var food = master.Body.RegisterTile.Matrix.Get<ItemAttributesV2>(master.Body.transform.localPosition.RoundToInt(), true).FirstOrDefault(IsInFoodPreferences);

			if (food is null)
			{
				var move = Directions[random.Next(0, Directions.Count)];
				master.Body.UniversalObjectPhysics.OrNull()?.TryTilePush(move.To2Int(), null);

				if (master.Body.Rotatable != null)
				{
					master.Body.Rotatable.SetFaceDirectionLocalVector(move.To2Int());
				}
			}
			else
			{
				SlimeEat.ClimbAndEat(food.gameObject);
			}
		}
		else
		{

			foreach (var player in players)
			{
				if (player.gameObject == master.Body.gameObject) continue;
				if (player.gameObject. GetComponent<LivingHealthMasterBase>().IsDead) continue;
				if (player.gameObject.GetComponent<LivingHealthMasterBase>().brain == null) continue;
				if (player.gameObject.GetComponent<LivingHealthMasterBase>().brain.GetComponent<SlimeCore>() != null) continue;

				if (GoodPlayers.ContainsKey(player.gameObject) == false)
				{
					GoodPlayers[player.gameObject] = 0;
				}

				GoodPlayers[player.gameObject]++;

				if (GoodPlayers[player.gameObject] < 300 && (player.transform.position - master.Body.transform.position).magnitude < 1.2)
				{
					SlimeEat.ClimbAndEat(player.gameObject);
					return;
				}
			}

			var move = Directions[random.Next(0, Directions.Count)];
			master.Body.UniversalObjectPhysics.OrNull()?.TryTilePush(move.To2Int(), null);

			if (master.Body.Rotatable != null)
			{
				master.Body.Rotatable.SetFaceDirectionLocalVector(move.To2Int());
			}
		}
	}


	public bool IsInFoodPreferences(ItemAttributesV2 food)
	{
		return food.gameObject.GetComponent<Edible>() != null;
	}


	public override bool HasGoal()
	{
		return false;
	}

}
