using System.Collections;
using UnityEngine;
using Mirror;
using Systems.MobAIs;

namespace Robotics
{
	public class InteractableBot : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>
	{
		HandApply interaction;

		[SerializeField] private SpriteRenderer SPRITE_RENDERER;
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
		public void ClientPredictInteraction(HandApply interaction) { }

		public void ServerRollbackClient(HandApply interaction) { }

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;

			return MobController != null;
		}
		public void ServerPerformInteraction(HandApply interaction)
		{
			this.interaction = interaction;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)
				&& interaction.HandObject.TryGetComponent<Emag>(out var emag)
				&& emag.EmagHasCharges())
			{
				TryEmag(emag, interaction);
			}
		}
		public void TryEmag(Emag emag, HandApply interaction)
		{
			if (MobController == null) return;
			MobController.IsEmagged = true;
			if (EMAGGED_SPRITE != null && SPRITE_RENDERER != null) SPRITE_RENDERER.sprite = EMAGGED_SPRITE;
			emag.UseCharge(interaction);
		}
	}
}
