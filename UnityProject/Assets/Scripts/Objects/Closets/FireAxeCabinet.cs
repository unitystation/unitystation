using Mirror;
using Systems.Clearance;
using UnityEngine;

namespace Objects.Wallmounts
{
	[RequireComponent(typeof(ItemStorage))]
	public class FireAxeCabinet : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{
		
		private ClearanceRestricted restricted;
		private Integrity integrity;
		private ItemStorage itemStorage;
		private ItemSlot axeSlot;
		
		[SerializeField] private SpriteHandler axeSpriteHandler;
		[SerializeField] private SpriteHandler glassSpriteHandler;
		[SerializeField] private SpriteHandler lockSpriteHandler;
		
		private bool isLocked = true;
		private bool isOpened = false;
		private bool isBroken = false;
		
		private const int OPEN_SPRITE = 5;
		private const float LOCK_TOGGLE_TIME = 2f;
		private const float GLASS_REPAIR_TIME = 2f;

		private void Awake()
		{
			restricted = GetComponent<ClearanceRestricted>();
			integrity = GetComponent<Integrity>();
			itemStorage = GetComponent<ItemStorage>();
			axeSlot = itemStorage.GetIndexedItemSlot(0);
		}
		
		public void OnSpawnServer(SpawnInfo info)
		{
			integrity.OnApplyDamage.AddListener(OnServerDamage);
		}

		private void OnDisable()
		{
			integrity.OnApplyDamage.RemoveListener(OnServerDamage);
		}
		
		private void OnServerDamage(DamageInfo info)
		{
			if (isBroken == false && integrity.integrity <= 0.2 * integrity.initialIntegrity)
			{
				isBroken = true;
			}
			
			if (isOpened == false)
			{
				UpdateGlassSprite();
			}
		}
		
		private void UpdateGlassSprite()
		{
				
				int index = Mathf.CeilToInt(integrity.integrity / integrity.initialIntegrity * 100f / 20);
				glassSpriteHandler.SetCatalogueIndexSprite(index - 1);					
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.Intent == Intent.Harm) return false;
			if (interaction.IsAltClick || interaction.HandObject == null ||
				Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) ||
				Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar) ||
				Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet) ||
				restricted.HasClearance(interaction.HandObject))
			{
				return true;
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick && isLocked == false)
			{
				ToggleOpen();
				return;
			}
			
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet) && isBroken)
			{
				DoRepair(interaction);
				return;
			}
			
			if (restricted.HasClearance(interaction.HandObject) && isOpened == false)
			{
				ToggleLock(interaction);
				return;
			}
			
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && isOpened == false)
			{
				ForceLock(interaction);
				return;
			}

			if (isOpened || isBroken)
			{
				bool isRemoving = interaction.HandObject == null;
				ItemSlot from = isRemoving ? axeSlot : interaction.HandSlot;
				ItemSlot to = isRemoving ? interaction.HandSlot : axeSlot;
				if (Inventory.ServerTransfer(from, to))
				{
					if (isRemoving)
					{
						axeSpriteHandler.PushClear();
					}
					else
					{
						axeSpriteHandler.PushTexture();
					}										
				}
			}
			else if (isLocked == false)
			{
				ToggleOpen();
			}
		}
		
		private void DoRepair(HandApply interaction)
		{
			if (interaction.UsedObject.TryGetComponent<Stackable>(out var stackable) && stackable.Amount > 2)
			{
				void ProgressFinishAction()
				{
					if (Inventory.ServerConsume(interaction.HandSlot, 2))
					{
						Chat.AddExamineMsg(interaction.Performer, $"You replace the glass.");
						integrity.RestoreIntegrity(integrity.initialIntegrity);
						isBroken = false;
						UpdateGlassSprite();
					}
				}
				
				var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Construction), ProgressFinishAction)
					.ServerStartProgress(interaction.Performer.RegisterTile(), GLASS_REPAIR_TIME , interaction.Performer);
	
				if (bar != null)
				{
					Chat.AddExamineMsg(interaction.Performer, "You begin replacing the glass.");
				}
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, $"You need more glass for that.");				
			}
		}
		
		private void ToggleLock(HandApply interaction)
		{
			isLocked = !isLocked;
			Chat.AddExamineMsg(interaction.Performer, $"You {(isLocked ? "lock" : "unlock")} the fire axe cabinet.");
			lockSpriteHandler.SetCatalogueIndexSprite(isLocked ? 0 : 1);			
		}
		
		private void ForceLock(HandApply interaction)
		{
			void ProgressFinishAction()
			{
				isLocked = !isLocked;
				Chat.AddExamineMsg(interaction.Performer, $"You {(isLocked ? "re-enable" : "disable")} the locking modules.");
				lockSpriteHandler.SetCatalogueIndexSprite(isLocked ? 0 : 1);
			}
			
			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.ItemTransfer), ProgressFinishAction)
				.ServerStartProgress(interaction.Performer.RegisterTile(), LOCK_TOGGLE_TIME, interaction.Performer);

			if (bar != null)
			{
				Chat.AddExamineMsg(interaction.Performer, "You begin resetting the locking modules.");
			}
		}
		
		private void ToggleOpen()
		{
			isOpened = !isOpened;
			
			if (isOpened)
			{
				glassSpriteHandler.SetCatalogueIndexSprite(OPEN_SPRITE);
			}
			else
			{
				UpdateGlassSprite();
			}
		}
	}		
}
