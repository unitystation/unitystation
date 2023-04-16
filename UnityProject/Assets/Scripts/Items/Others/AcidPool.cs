using System;
using System.Timers;
using UnityEngine;

namespace Items.Others
{
	public class AcidPool : MonoBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private float acidDamage = 10f;

		private UniversalObjectPhysics objectPhysics;
		private RegisterTile registerTile => objectPhysics.registerTile;

		private float timer;

		private const float UpdateTime = 1f;

		private const float DespawnTime = 60f;

		private void Awake()
		{
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			UpdateManager.Add(OnUpdate, UpdateTime);
			GetComponent<Pickupable>().ServerSetCanPickup(false);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
		}

		private void OnUpdate()
		{
			timer += UpdateTime;

			if (timer > DespawnTime)
			{
				Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} fizzles out.");
				_ = Despawn.ServerSingle(gameObject);
				return;
			}

			var localPos = objectPhysics.OfficialPosition.ToLocalInt(registerTile.Matrix);

			ObjectDamage(localPos);

			TileDamage(localPos);

			if(DMMath.Prob(75)) return;

			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} fizzes.");
		}

		private void ObjectDamage(Vector3Int localPos)
		{
			var stuffOnTile = registerTile.Matrix.GetAs<RegisterTile>(localPos, true);

			foreach (var thing in stuffOnTile)
			{
				if(thing.TryGetComponent<Integrity>(out var integrity) == false) continue;
				if(integrity.Resistances.AcidProof) continue;
				if(integrity.Resistances.UnAcidable) continue;
				if(integrity.Resistances.Indestructable) continue;

				integrity.ApplyDamage(acidDamage, AttackType.Acid, DamageType.Brute);
			}
		}

		private void TileDamage(Vector3Int localPos)
		{
			var hasTile = registerTile.Matrix.MetaTileMap.HasTile(localPos, LayerType.Walls);

			if (hasTile == false)
			{
				hasTile = registerTile.Matrix.MetaTileMap.HasTile(localPos, LayerType.Windows);
			}

			if (hasTile == false)
			{
				hasTile = registerTile.Matrix.MetaTileMap.HasTile(localPos, LayerType.Grills);
			}

			if(hasTile == false) return;

			registerTile.Matrix.MetaTileMap.ApplyDamage(localPos, acidDamage, objectPhysics.OfficialPosition.RoundToInt(),
				AttackType.Acid);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandObject != null) return false;

			if (DefaultWillInteract.Default(interaction, side, PlayerTypes.Alien) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Aliens can remove acid pools by clicking on them
			Chat.AddActionMsgToChat(interaction.Performer, $"You wipe away the {gameObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()} wipes away the {gameObject.ExpensiveName()}");

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}