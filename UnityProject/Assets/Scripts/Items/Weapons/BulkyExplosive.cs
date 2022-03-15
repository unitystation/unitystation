using UnityEngine;

namespace Items.Weapons
{
	/// <summary>
	/// Interaction script for Bulky explosives that cannot be picked up and instead pulled.
	/// </summary>
	public class BulkyExplosive : ExplosiveBase, ICheckedInteractable<HandApply>
	{
		[SerializeField] private ItemTrait wrenchTrait;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (explosiveType != ExplosiveType.SyndicateBomb ||
			    DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject != null && interaction.HandObject.Item().HasTrait(wrenchTrait))
			{
				pushPull.ServerSetAnchored(pushPull.IsPushable, interaction.Performer);
				var wrenchText = pushPull.IsPushable ? "wrench down" : "unwrench";
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Wrench, gameObject.AssumedWorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, $"You {wrenchText} the {gameObject.ExpensiveName()}");
				return;
			}
			explosiveGUI.ServerPerformInteraction(interaction);
		}
	}
}