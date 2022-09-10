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
		[SerializeField] private float timeToGib = 12f;
		[SerializeField] private int produceMultiplier = 4;
		[SerializeField, Tooltip("In watts.")] private int powerUseWhenActive = 1000;
		[SerializeField] private int damagePerFrame = 20;
		[SerializeField] private GameObject defaultProduce;
		[SerializeField] private SpriteHandler lights;
		[SerializeField] private SpriteDataSO lightsoff;
		[SerializeField] private SpriteDataSO lightson;

		private bool isRunning = false;

		private Dictionary<GameObject, int> gibbed = new Dictionary<GameObject, int>();

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

			isRunning = !isRunning;
			if (isRunning) StartCoroutine(GibbingTime());
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

		private IEnumerator GibbingTime()
		{
			Chat.AddLocalMsgToChat("The gibber violently shakes as it shreds everything inside of it.", gameObject);
			lights.SetSpriteSO(lightson);
			int time = 0;
			while (isRunning)
			{
				if(timeToGib <= time) break;
				//Always update the list to avoid collection NREs when moving things in and out of storage.
				time++;
				yield return WaitFor.Seconds(0.6f);
				yield return CheckContentAndHarm();
			}

			storage.RetrieveObjects();
			foreach (var products in gibbed)
			{
				_ = Spawn.ServerPrefab(products.Key, gameObject.TileWorldPosition().To3(), gameObject.RegisterTile().Matrix.transform,
					null, Mathf.Min(1, products.Value));
			}
			gibbed.Clear();
			isRunning = false;
			Chat.AddLocalMsgToChat("The gibber stops vibrating as it finishes it's operation.", gameObject);
			lights.SetSpriteSO(lightsoff);
		}

		private IEnumerator CheckContentAndHarm()
		{
			var list = storage.GetStoredObjects().ToArray();
			foreach (var slot in list)
			{
				if (slot.TryGetComponent<LivingHealthMasterBase>(out var gib))
				{
					gib.ApplyDamageAll(gameObject, gib.PainScreamDamage + damagePerFrame,
						AttackType.Melee, DamageType.Brute, true);
					if (gib.OverallHealth > -100) continue;
					var meatToProduce = gib.MeatProduce != null ? gib.MeatProduce : defaultProduce;
					var skinToProduce = gib.SkinProduce != null ? gib.SkinProduce : defaultProduce;
					if (gibbed.ContainsKey(meatToProduce))
					{
						gibbed[meatToProduce] += 1 * produceMultiplier;
					}
					else
					{
						gibbed.Add(meatToProduce, 1);
					}

					if (gibbed.ContainsKey(skinToProduce))
					{
						gibbed[skinToProduce] += 1 * produceMultiplier;
					}
					else
					{
						gibbed.Add(skinToProduce, 1);
					}

					storage.RemoveObject(slot);
					yield return WaitFor.EndOfFrame;
					gib.OnGib();
					continue;
				}

				if (slot.TryGetComponent<LivingHealthBehaviour>(out var oldMob))
				{
					oldMob.Death();
					if (gibbed.ContainsKey(defaultProduce))
					{
						gibbed[defaultProduce] += 1 * produceMultiplier;
					}
					else
					{
						gibbed.Add(defaultProduce, 1 * produceMultiplier);
					}
					continue;
				}
				if (slot.TryGetComponent<Integrity>(out var integrity) == false) continue;
				if (integrity.gameObject.Item() != null) continue;
				//Non-meaty items shall be damaged.. in exchange of damaging the machine itself.
				machineIntegrity.ApplyDamage(damagePerFrame / 2, AttackType.Melee, DamageType.Brute,
					false, false, false, true);
				integrity.ApplyDamage(damagePerFrame, AttackType.Melee, DamageType.Brute);
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