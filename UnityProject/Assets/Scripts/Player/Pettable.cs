using HealthV2;
using Logs;
using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// Allows an object to be pet by a player. Shameless copy of Huggable.cs
	/// </summary>
	public class Pettable : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject != null) return false;
			if (interaction.Intent != Intent.Help) return false;

			if (interaction.TargetObject.TryGetComponent<LivingHealthMasterBase>(out var healthV2))
			{
				return (healthV2.IsDead || healthV2.IsCrit || healthV2.IsSoftCrit) == false;
			}
			// fallback to old system
			// TODO: convert all mobs to new system then remove this
			else if (interaction.TargetObject.TryGetComponent<Mob.SimpleAnimal>(out var health))
			{
				return (health.IsDead || health.IsCrit || health.IsSoftCrit) == false;
			}

			Loggy.LogError($"{this} is missing a health component. Cannot pet this mob.");
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			string npcName = gameObject.ExpensiveName();
			if (TryGetComponent<MobAI>(out var npc))
			{
				npc.OnPetted(interaction.Performer.gameObject);
				if (string.IsNullOrWhiteSpace(npc.mobName) == false)
				{
					npcName = npc.mobName;
				}

			}

			Chat.AddActionMsgToChat(
				interaction.Performer,
				$"You pet {npcName}.",
				$"{interaction.Performer.ExpensiveName()} pets {npcName}.");
		}
	}
}
