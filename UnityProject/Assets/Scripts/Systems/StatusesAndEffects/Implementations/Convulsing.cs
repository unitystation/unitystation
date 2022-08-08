using System;
using Core.Chat;
using Systems.StatusesAndEffects.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Systems.StatusesAndEffects.Implementations
{
	[CreateAssetMenu(fileName = "Convulsing", menuName = "ScriptableObjects/StatusEffects/Convulsing")]
	public class Convulsing: StatusEffect, IStackableStatus, IExpirableStatus
	{
		private const int MAX_STACKS = 5;
		private const int BASE_PROC_CHANCE = 20;
		[SerializeField] private Vector2 triggerTimeRange = new(5f, 10f);
		public float duration = 20f;

		public event Action<IExpirableStatus> Expired;
		public float Duration => duration;
		public int InitialStacks { get; set; } = 1;
		public int Stacks { get; set; }
		public DateTime DeathTime { get; set; }

		private int actualProcChance;
		private DateTime nextTriggerTime;
		private float Timer => Random.Range(triggerTimeRange.x, triggerTimeRange.y);

		public override void OnAdded()
		{
			Stacks = InitialStacks;
			actualProcChance = BASE_PROC_CHANCE;
			nextTriggerTime = DateTime.Now.AddSeconds(Timer);
			DeathTime = DateTime.Now.AddSeconds(duration);
			UpdateManager.Add(Trigger, 1f);
			UpdateManager.Add(CheckExpiration, 1f);
		}

		public override void OnRemoved()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Trigger);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckExpiration);
		}

		public void AddStack(int amount)
		{
			Stacks = Mathf.Clamp(Stacks + amount, 0, MAX_STACKS);
			actualProcChance = BASE_PROC_CHANCE * Stacks;
		}

		public void RemoveStack(int amount)
		{
			Stacks -= amount;
			actualProcChance = BASE_PROC_CHANCE * Stacks;
		}


		private void DropItemFromHands(NamedSlot namedSlot, PlayerScript player)
		{
			foreach (var slot in player.DynamicItemStorage.GetNamedItemSlots(namedSlot))
			{
				var item = slot.Item;
				if (item != null)
				{
					Chat.AddActionMsgToChat(
						target,
						$"You convulse violently, and you drop {item.gameObject.ExpensiveName()}!",
						$"{target.ExpensiveName()} convulses, and they drop their {item.gameObject.ExpensiveName()}!");
				}
				else
				{
					Chat.AddActionMsgToChat(
						target,
						$"You convulse violently!",
						$"{target.ExpensiveName()} convulses!");
				}
				Inventory.ServerDrop(slot);
			}
		}


		public override void DoEffect()
		{
			if (target.TryGetComponent<PlayerScript>(out var player) == false)
			{
				return;
			}

			if (DMMath.Prob(actualProcChance))
			{
				DropItemFromHands(NamedSlot.leftHand, player);
				DropItemFromHands(NamedSlot.rightHand, player);
			}
			else
			{
				Chat.AddActionMsgToChat(
					target,
					"You convulse, but you maintain your composure!",
					$"{target.ExpensiveName()} convulses!");
			}

			EmoteActionManager.DoEmote("twitch", target);
		}

		private void Trigger()
		{
			if (DateTime.Now < nextTriggerTime) return;
			nextTriggerTime = DateTime.Now.AddSeconds(Timer);
			DoEffect();
		}

		public void CheckExpiration()
		{
			if (DateTime.Now > DeathTime)
			{
				Expired?.Invoke(this);
			}
		}
	}
}