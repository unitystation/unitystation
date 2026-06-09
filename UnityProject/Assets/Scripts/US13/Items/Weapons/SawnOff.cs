using UnityEngine;
using US13.Core.Camera;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.HealthV2;
using US13.Items.Traits;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Weapons
{
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(Gun))]
	public class SawnOff : MonoBehaviour, ICheckedInteractable<InventoryApply>, IServerSpawn
	{

		private ItemAttributesV2 itemAttComp;
		private Gun gunComp;
		private SpriteHandler spriteHandler;

		private bool isSawn = false;

		[SerializeField, Tooltip("Determines if the gun will explode in the users face when sawn while it still contains ammo")]
		private bool ammoBackfire = true;

		[SerializeField, Tooltip("Sawn off item size")]
		private Size sawnSize = Size.Medium;

		[SerializeField, Tooltip("Value that determines how far a shot will deviate. (Never set this higher then 0.5 unless you want questionable results.)")]
		private float sawnMaxRecoilVariance;

		[SerializeField, Tooltip("Recoil camera config to switch to when sawn off")]
		private CameraRecoilConfig SawnCameraRecoilConfig;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			itemAttComp = gameObject.GetComponent<ItemAttributesV2>();
			gunComp = gameObject.GetComponent<Gun>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			spriteHandler.SetCatalogueIndexSprite(0);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only reload if the gun is the target and tool is in hand slot
			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
			{
				//TODO: switch this trait to the circular saw when that is implemented
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder))
				{
					return true;
				}
			}
			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			//TODO: switch this trait to the circular saw when that is implemented
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder) && gunComp.ShotCooldown == false)
			{
				if (isSawn)
				{
					Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} is already shortened!");
				}

				if (ammoBackfire && gunComp.CurrentMagazine.ServerAmmoRemains != 0)
				{
					gunComp.ServerShoot(interaction.Performer, Vector2.zero, BodyPartType.Head, true,interaction.Performer.gameObject );
					Chat.AddActionMsgToChat(interaction.Performer,
					$"The {gameObject.ExpensiveName()} goes off in your face!",
					$"The {gameObject.ExpensiveName()} goes off in {interaction.Performer.ExpensiveName()}'s face!");
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You shorten the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.ExpensiveName()} shortens the {gameObject.ExpensiveName()}");

					itemAttComp.ServerSetSize(sawnSize);
					spriteHandler.SetCatalogueIndexSprite(1);

					// Don't overwrite recoil conf if it isn't setup
					if (SawnCameraRecoilConfig.Distance != 0f)
					{
						gunComp.SyncCameraRecoilConfig(gunComp.CameraRecoilConfig, SawnCameraRecoilConfig);
					}

					gunComp.MaxRecoilVariance = sawnMaxRecoilVariance;
					isSawn = true;
				}
			}

			// Propagates the InventoryApply Interaction to the Gun component for all basic gun InventoryApply interactions.
			gunComp.ServerPerformInteraction(interaction);
		}
	}
}