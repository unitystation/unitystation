using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using AddressableReferences;
using ScriptableObjects;
using Chemistry;
using Chemistry.Components;
using Items;

namespace Chemistry
{
	public class ChemGrenade : MonoBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>, IExaminable
	{
		public int TimerLength = 5000;

		private AddressableAudioSource armedSound = CommonSounds.Instance.BeepBeep;

		private const int UNSECURED_SPRITE = 0;
		private const int SECURED_SPRITE = 1;
		private const int ACTIVE_SPRITE = 2;

		public SpriteHandler[] spriteHandlers = new SpriteHandler[] { };

		private ItemStorage itemStorage;
		private ItemSlot containerA;
		private ItemSlot containerB;
		//private ItemSlot detonator;

		private ReagentContainer reactionChamber;

		private bool isSecured = false;
		private bool isActive = false;

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
			reactionChamber = GetComponent<ReagentContainer>();
			containerA = itemStorage.GetIndexedItemSlot(0);
			containerB = itemStorage.GetIndexedItemSlot(1);
		}

		private void UpdateSprites()
		{
			foreach (var handler in spriteHandlers)
			{
				if (handler)
				{
					int newSpriteID = isSecured ? SECURED_SPRITE : UNSECURED_SPRITE;
					newSpriteID = isActive ? ACTIVE_SPRITE : newSpriteID;
					handler.ChangeSprite(newSpriteID);
				}
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			string desc = "";
			int containerAmount = 0;

			containerAmount = containerA.ItemObject != null ? containerAmount + 1 : containerAmount + 0;
			containerAmount = containerB.ItemObject != null ? containerAmount + 1 : containerAmount + 0;

			desc += $"It has "+containerAmount.ToString()+" containers installed.\n";
			if (isSecured) desc += $"Cover is secured.\n";
			if (isActive) desc += $"<color=red> It's primed!";

			return $"{gameObject.GetComponent<ItemAttributesV2>().InitialDescription}" + desc;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false || isActive) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.ReagentContainer) && isSecured == false) return true; //inserting beakers

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter) && isSecured == false) return true; //changing timer

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return true; //securing/unsecuring

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.ReagentContainer) && isSecured == false)
			{
				ItemSlot transferSlot = null;
				if (containerA == null) transferSlot = containerA;
				else if (containerB == null) transferSlot = containerB;

				if (transferSlot != null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You insert container into casing.");
					Inventory.ServerTransfer(interaction.HandSlot, transferSlot);
				}
				else Chat.AddExamineMsgFromServer(interaction.Performer, $"You can't another insert container into casing, eject old ones first.");
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter) && isSecured == false)
			{
				UIManager.Instance.GrenadeTimerMenu.Enable();
				UIManager.Instance.GrenadeTimerMenu.Grenade = gameObject;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
			{
				isSecured = !isSecured;
				UpdateSprites();
			}
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false || isActive) return false;

			if (Validations.HasItemTrait(interaction.FromSlot.ItemObject, CommonTraits.Instance.ReagentContainer) && isSecured == false) return true; //inserting beakers

			if (Validations.HasItemTrait(interaction.FromSlot.ItemObject, CommonTraits.Instance.Wirecutter) && isSecured == false) return true; //changing timer

			if (Validations.HasItemTrait(interaction.FromSlot.ItemObject, CommonTraits.Instance.Screwdriver)) return true; //securing/unsecuring

			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (Validations.HasItemTrait(interaction.FromSlot.ItemObject, CommonTraits.Instance.ReagentContainer) && isSecured == false)
			{
				ItemSlot transferSlot = null;
				if (containerA == null) transferSlot = containerA;
				else if (containerB == null) transferSlot = containerB;

				if (transferSlot != null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You insert container into casing.");
					Inventory.ServerTransfer(interaction.FromSlot, transferSlot);
				}
				else Chat.AddExamineMsgFromServer(interaction.Performer, $"You can't another insert container into casing, eject old ones first.");
			}

			if (Validations.HasItemTrait(interaction.FromSlot.ItemObject, CommonTraits.Instance.Wirecutter) && isSecured == false)
			{
				UIManager.Instance.GrenadeTimerMenu.Enable();
				UIManager.Instance.GrenadeTimerMenu.Grenade = gameObject;
			}

			if (Validations.HasItemTrait(interaction.FromSlot.ItemObject, CommonTraits.Instance.Screwdriver))
			{
				isSecured = !isSecured;
				UpdateSprites();
			}
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false || isActive) return false;

			return false;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (isSecured && containerA.ItemObject != null && containerA.ItemObject != null)
			{
				isActive = true;
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You prime the grenade!");
				Activate();
				UpdateSprites();
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You eject all the containers from casing.");
				Inventory.ServerDrop(containerA);
				Inventory.ServerDrop(containerB);
			}
		}

		public async Task Activate()
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(armedSound, gameObject.GetComponent<Transform>().position);
			Task.Delay(TimerLength);

			ReagentContainer conA = containerA.ItemObject.GetComponent<ReagentContainer>();
			ReagentContainer conB = containerB.ItemObject.GetComponent<ReagentContainer>();

			var resA = conA.MoveReagentsTo(conA.CurrentReagentMix.Total, reactionChamber);
			var resB = conB.MoveReagentsTo(conB.CurrentReagentMix.Total, reactionChamber);
			reactionChamber.CurrentReagentMix.Temperature += 373.15f; //100°C

			Task.Delay(30);
			if (gameObject == null) return;
			Despawn.ServerSingle(gameObject); //in case if reaction haven't happened at all/isn't despawning container on finish
		}
	}
}
