using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.NPC.AI;
using US13.Systems.Inventory;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.NPC.Mood
{
	[RequireComponent(typeof(MobAI))]
	[RequireComponent(typeof(MobMood))]
	public class FeedableMob: MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private MobAI mobAI;
		private MobMood mood;
		private MobExplore mobExplore;

		private void Awake()
		{
			mobAI = GetComponent<MobAI>();
			mood = GetComponent<MobMood>();
			mobExplore = GetComponent<MobExplore>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       !mobAI.health.IsDead &&
			       !mobAI.health.IsCrit &&
			       !mobAI.health.IsSoftCrit &&
			       interaction.Intent == Intent.Help &&
			       interaction.HandObject != null &&
			       mobExplore.IsInFoodPreferences(interaction.HandObject);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Inventory.ServerConsume(interaction.HandSlot, 1);
			mood.OnFoodEaten();
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.EatFood,
				gameObject.RegisterTile().WorldPosition, audioSourceParameters,	sourceObj: gameObject);

			Chat.AddActionMsgToChat(
				interaction.Performer,
				$"You feed {mobAI.mobName.Capitalize()} with {interaction.HandObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()}" +
				$" feeds some {interaction.HandObject.ExpensiveName()} to {mobAI.mobName.Capitalize()}");
		}
	}
}