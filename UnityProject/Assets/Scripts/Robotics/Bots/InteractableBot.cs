using System.Collections;
using UnityEngine;
using Mirror;
using Systems.MobAIs;
using Items;

namespace Robotics
{
	public class InteractableBot : NetworkBehaviour, ICheckedInteractable<HandApply>
	{

		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private Sprite EMAGGED_SPRITE;

		private MobExplore mobController;
		public MobExplore MobController
		{
			get
			{
				if (!mobController)
				{
					mobController = GetComponent<MobExplore>();
				}
				return mobController;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;

			return MobController != null;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)
				&& interaction.HandObject.TryGetComponent<Emag>(out var emag)
				&& emag.EmagHasCharges())
			{
				PerformEmag(emag, interaction);
			}
		}

		public void PerformEmag(Emag emag, HandApply interaction)
		{
			if (MobController == null) return;
			MobController.IsEmagged = true;
			if (EMAGGED_SPRITE != null && spriteHandler != null) spriteHandler.SetSprite(EMAGGED_SPRITE);
			emag.UseCharge(interaction);
		}
	}
}
