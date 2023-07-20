using System.Collections;
using HealthV2;
using UnityEngine;

namespace Items
{
	public class SupermatterSliver : MonoBehaviour, IServerInventoryMove, ICheckedInteractable<HandApply>, ISuicide
	{
		[SerializeField]
		private ItemTrait supermatterScalpel = null;

		[SerializeField]
		private ItemTrait supermatterTongs = null;

		public bool vaporizeWhenPickedUp = true;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject == null) return false;

			//Dont vaporize unvaporizible
			if (Validations.HasItemTrait(interaction.HandObject, supermatterScalpel)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, supermatterTongs))
			{
				if (interaction.HandObject.TryGetComponent<SupermatterTongs>(out var smTongs))
				{
					smTongs.LoadSliver(gameObject.GetComponent<Pickupable>());
				}
			}
			else
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You touch the {gameObject.DisplayName()} with the {interaction.HandObject.DisplayName()}, and everything suddenly goes silent.\n The {interaction.HandObject.DisplayName()} flashes into dust.",
					$"As {interaction.Performer.DisplayName()} touches the {gameObject.DisplayName()} with {interaction.HandObject.DisplayName()}, silence fills the room...");
				_ = Despawn.ServerSingle(interaction.UsedObject);
			}
		}

		//Turn player into ash if he picked it up
		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (this.gameObject != info.MovedObject.gameObject) return;

			if (info.InventoryMoveType != InventoryMoveType.Add) return;

			if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
			{
				var player = info.ToRootPlayer?.PlayerScript;

				if (player != null && vaporizeWhenPickedUp)
				{
					Chat.AddActionMsgToChat(player.PlayerChatLocation,
						$"You reach for the {gameObject.DisplayName()} with your hands. That was dumb.",
						$"{player.visibleName} touches {gameObject.DisplayName()} with bare hands. His body bursts into flames and flashes to dust after few moments.");

					player.playerHealth.OnGib();
				}
			}
		}

		public bool CanSuicide(GameObject performer)
		{
			return vaporizeWhenPickedUp;
		}

		public IEnumerator OnSuicide(GameObject performer)
		{
			yield return WaitFor.FixedUpdate;
			Chat.AddActionMsgToChat(gameObject, $"{performer.DisplayName()} mistook the {gameObject.DisplayName()} for a tasty snack. Yumm..");
			gameObject.Player().Script.playerHealth.OnGib();
		}
	}
}
