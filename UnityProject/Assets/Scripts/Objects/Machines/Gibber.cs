using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Mirror;
using NaughtyAttributes;
using Systems.Electricity;
using UnityEngine;

namespace Objects.Machines
{
	public class Gibber : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<MouseDrop>, IExaminable, IRightClickable
	{
		[SerializeField] private ObjectContainer storage;
		[SerializeField] private UniversalObjectPhysics physics;
		[SerializeField] private Integrity machineIntegrity;
		[SerializeField] private int numberOfTimesToDamage = 8;
		[SerializeField] private int produceMultiplier = 4;
		[SerializeField] private int damagePerFrame = 20;
		[SerializeField] private GameObject defaultProduce;
		[SerializeField] private SpriteHandler lights;
		[SerializeField] private SpriteDataSO lightsoff;
		[SerializeField] private SpriteDataSO lightson;

		private bool isRunning = false;
		private int damageNumber = 0;

		private Dictionary<GameObject, int> gibbed = new Dictionary<GameObject, int>();

		private const float DAMAGE_TIME = 1.8f;
		private const int HALF = 2;
		private const float LOW_HEALTH = -75f;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			// Only interact with this if it's anchored.
			if (physics.isNotPushable == false) return false;
			//Checks if the player is in reach + if they are able to interact with this type of object.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			//For people attacking the object so they don't accidentally trip it.
			return interaction.Intent != Intent.Harm;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject != null)
			{
				storage.StoreObject(interaction.HandObject);
				return;
			}

			if (storage.IsEmpty)
			{
				Chat.AddExamineMsg(interaction.Performer, "This Gibber has nothing inside of it.");
				return;
			}

			isRunning = !isRunning;
			if (isRunning)
			{
				StartGibbing();
			}
			else
			{
				StopGibbing();
			}
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			// Only interact with this if it's anchored.
			if (physics.isNotPushable == false) return false;
			//Checks if the player is in reach + if they are able to interact with this type of object.
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{
			storage.StoreObject(interaction.DroppedObject);
		}

		private void StartGibbing()
		{
			Chat.AddLocalMsgToChat("The gibber violently shakes as it shreds everything inside of it.", gameObject);
			lights.SetSpriteSO(lightson);
			UpdateManager.Add(CheckContentAndHarm, DAMAGE_TIME);
		}

		private void StopGibbing()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckContentAndHarm);
			Chat.AddLocalMsgToChat("The gibber stops vibrating as it finishes its operation.", gameObject);
			lights.SetSpriteSO(lightsoff);
			storage.RetrieveObjects();

			foreach (var products in gibbed)
			{
				_ = Spawn.ServerPrefab(products.Key, gameObject.TileWorldPosition().To3(), gameObject.RegisterTile().Matrix.transform,
					null, Mathf.Max(1, products.Value));
			}
			gibbed.Clear();
			isRunning = false;
			numberOfTimesToDamage = 0;
			damageNumber = 0;
		}

		private void CheckContentAndHarm()
		{
			damageNumber++;
			foreach (var slot in storage.GetStoredObjects().Reverse())
			{
				if (slot.TryGetComponent<LivingHealthMasterBase>(out var gib))
				{
					gib.ApplyDamageAll(gameObject, gib.PainScreamDamage + damagePerFrame,
						AttackType.Melee, DamageType.Brute, true);
					if (gib.OverallHealth > LOW_HEALTH) continue;
					var meatToProduce = gib.MeatProduce.OrNull() ?? defaultProduce;
					var skinToProduce = gib.SkinProduce.OrNull() ?? defaultProduce;
					AddItemsThatWillBeSpawned(meatToProduce, skinToProduce);
					storage.RetrieveObject(slot ,null, gib.OnGib);
					continue;
				}

				if (slot.TryGetComponent<LivingHealthBehaviour>(out var oldMob))
				{
					oldMob.Death();
					AddItemsThatWillBeSpawned(defaultProduce);

					continue;
				}
				if (slot.TryGetComponent<Integrity>(out var integrity) == false) continue;
				if (integrity.gameObject.Item() != null) continue;
				//Non-meaty items shall be damaged.. in exchange of damaging the machine itself.
				machineIntegrity.ApplyDamage(damagePerFrame / HALF, AttackType.Melee, DamageType.Brute,
					false, false, false, true);
				integrity.ApplyDamage(damagePerFrame, AttackType.Melee, DamageType.Brute);
			}
			if (damageNumber > numberOfTimesToDamage) StopGibbing();
		}

		private void AddItemsThatWillBeSpawned(GameObject meat, GameObject skin = null)
		{
			if (gibbed.ContainsKey(meat))
			{
				gibbed[meat] += 1 * produceMultiplier;
			}
			else
			{
				gibbed.Add(meat, 1 * produceMultiplier);
			}
			if(skin == null) return;
			if (gibbed.ContainsKey(skin))
			{
				gibbed[skin] += 1 * produceMultiplier;
			}
			else
			{
				gibbed.Add(skin, 1 * produceMultiplier);
			}
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (isRunning == false) return "The display reads 'Waiting..'";
			var totalMeat = gibbed.Values.Sum();
			return $"The display reads 'Current Output: {totalMeat}'";
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var rightClickResult = new RightClickableResult();
			if (isRunning) return rightClickResult;
			rightClickResult.AddElement("Eject Content", () => storage.RetrieveObjects());
			return rightClickResult;
		}
	}
}